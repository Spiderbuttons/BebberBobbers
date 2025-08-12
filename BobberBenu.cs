using System;
using System.Collections.Generic;
using System.Linq;
using BebberBobbers.Helpers;
using Force.DeepCloner;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.BellsAndWhistles;
using StardewValley.Extensions;
using StardewValley.Menus;
using StardewValley.Tools;

namespace BebberBobbers;

public class BobberBenu : IClickableMenu
{
    private static readonly Rectangle ICON_BACK_RECTANGLE = new(222, 317, 16, 16);

    private static readonly Point ICON_BACK_HIGHLIGHT_POSITION = new(256, 317);

    private const int ICON_WIDTH = 68;

    public readonly List<ClickableTextureComponent> icons = [];

    public readonly List<ClickableTextureComponent> iconFronts = [];

    private readonly int ICON_X_OFFSET = ICON_WIDTH / 2 - ICON_BACK_RECTANGLE.Width * 4 / 2 - 4;

    private int selected = -1;

    private const string RANDOM_BOBBER_INDEX = "-2";

    private const string LEFT_ARROW_INDEX = "-3";
    private const string RIGHT_ARROW_INDEX = "-4";

    private int pageNumber = 0;

    private bool atMinPage = true;
    private bool atMaxPage = true;

    public BobberBenu()
    {
        setUpIcons();
    }

    public override void gameWindowSizeChanged(Rectangle oldBounds, Rectangle newBounds)
    {
        base.gameWindowSizeChanged(oldBounds, newBounds);
        setUpIcons();
    }

    private void addVanillaBobber(int bobberIndex)
    {
        int available = Game1.player.fishCaught.Count() / 2;
        bool ghosted = bobberIndex > available;
        Rectangle src = Game1.getSourceRectForStandardTileSheet(Game1.bobbersTexture, bobberIndex, 16, 32);
        src.Height = 16;
        icons.Add(new ClickableTextureComponent(new Rectangle(0, 0, 64, 64), Game1.mouseCursors_1_6,
            ICON_BACK_RECTANGLE, 4f, drawShadow: true)
        {
            name = bobberIndex.ToString()
        });
        if (ghosted)
        {
            iconFronts.Add(new ClickableTextureComponent(new Rectangle(0, 0, 16, 16),
                Game1.mouseCursors_1_6, new Rectangle(272, 317, 16, 16), 4f)
            {
                name = "ghosted"
            });
        }
        else
        {
            iconFronts.Add(new ClickableTextureComponent(new Rectangle(0, 0, 16, 16), Game1.bobbersTexture,
                src, 4f, drawShadow: true));
        }
    }

    public void setUpIcons()
    {
        icons.Clear();
        iconFronts.Clear();

        if (Game1.player.usingRandomizedBobber)
        {
            Game1.player.bobberStyle.Value = -2;
        }

        width = Math.Max(800, Game1.uiViewport.Width / 3);
        xPositionOnScreen = Game1.uiViewport.Width / 2 - width / 2;
        height = 100;

        int maxIconsPerRow = width / ICON_WIDTH;
        int maxRows = 5;
        int maxIcons = maxIconsPerRow * maxRows;

        int iconSpacing = 4;
        var customBobberList = ModEntry.Bobbers;

        for (int i = 0; i < 40; i++)
        {
            addVanillaBobber(i);
        }

        foreach (var customBobber in customBobberList)
        {
            if (customBobber.Texture == null)
            {
                Log.Warn($"Skipping custom bobber {customBobber.Id} due to missing texture.");
                continue;
            }

            Texture2D bobberTexture = Game1.content.Load<Texture2D>(customBobber.Texture);
                bool ghosted = !GameStateQuery.CheckConditions(customBobber.Condition);
                Rectangle src = Game1.getSourceRectForStandardTileSheet(bobberTexture, customBobber.SpriteIndex, 16, 32);
                src.Height = 16;
                icons.Add(new ClickableTextureComponent(new Rectangle(0, 0, 64, 64), Game1.mouseCursors_1_6,
                    ICON_BACK_RECTANGLE, 4f, drawShadow: true)
                {
                    name = icons.Count.ToString()
                });
                if (ghosted)
                {
                    iconFronts.Add(new ClickableTextureComponent(new Rectangle(0, 0, 16, 16),
                        Game1.mouseCursors_1_6, new Rectangle(272, 317, 16, 16), 4f)
                    {
                        name = "ghosted"
                    });
                }
                else
                    iconFronts.Add(new ClickableTextureComponent(new Rectangle(0, 0, 16, 16),
                        bobberTexture, src, 4f, drawShadow: true));
        }

        icons.Add(new ClickableTextureComponent(new Rectangle(0, 0, 64, 64), null, new Rectangle(0, 0, 0, 0),
            4f, drawShadow: true)
        {
            name = RANDOM_BOBBER_INDEX,
            myID = -69
        });
        iconFronts.Add(new ClickableTextureComponent(new Rectangle(0, 0, 10, 10), Game1.mouseCursors_1_6,
            new Rectangle(496, 28, 16, 16), 4f, drawShadow: true));

        icons.Add(new ClickableTextureComponent(
            new Rectangle(0, 0, 12, 11), null,
            new Rectangle(352, 495, 12, 11), 4f)
        {
            name = LEFT_ARROW_INDEX,
            myID = -70
        });
        iconFronts.Add(new ClickableTextureComponent(
            new Rectangle(0, 0, 12, 11), Game1.mouseCursors,
            new Rectangle(352, 495, 12, 11), 4f));
        icons.Add(new ClickableTextureComponent(
            new Rectangle(0, 0, 12, 11), null,
            new Rectangle(365, 495, 12, 11), 4f)
        {
            name = RIGHT_ARROW_INDEX,
            myID = -71
        });
        iconFronts.Add(new ClickableTextureComponent(
            new Rectangle(0, 0, 12, 11), Game1.mouseCursors,
            new Rectangle(365, 495, 12, 11), 4f));

        selected = Game1.player.bobberStyle.Value;

        height += (icons.Count * ICON_WIDTH / width + 1) * (icons[0].bounds.Height + iconSpacing);
        yPositionOnScreen = Game1.uiViewport.Height / 2 - height / 2;
        int y = yPositionOnScreen + 100;
        int startIndex = pageNumber * (maxIconsPerRow * maxRows);
        int indexInPage = 0;
        for (int iconIndex = startIndex; iconIndex < icons.Count; iconIndex += maxIconsPerRow)
        {
            int rowNumber = iconIndex / maxIconsPerRow + 1;
            int rowCount = Math.Min(icons.Count - iconIndex, maxIconsPerRow);
            int x = xPositionOnScreen + width / 2 - maxIconsPerRow * ICON_WIDTH / 2;
            for (int iconInRow = 0; iconInRow < rowCount; iconInRow++)
            {
                int index = iconInRow + iconIndex;
                icons[index].bounds.X = x + iconInRow * ICON_WIDTH;
                icons[index].bounds.Y = y;
                icons[index].bounds.Width = ICON_WIDTH;
                
                if (icons[index].name.Equals(RANDOM_BOBBER_INDEX) || icons[index].name.Equals(LEFT_ARROW_INDEX) || icons[index].name.Equals(RIGHT_ARROW_INDEX))
                {
                    icons[index].bounds.X = xPositionOnScreen + width / 2 - maxIconsPerRow * ICON_WIDTH / 2;
                    if (icons[index].name.Equals(LEFT_ARROW_INDEX)) 
                        icons[index].bounds.X += ICON_WIDTH;
                    else if (icons[index].name.Equals(RIGHT_ARROW_INDEX))
                        icons[index].bounds.X += ICON_WIDTH * (maxIconsPerRow - 1);
                    icons[index].bounds.Y = yPositionOnScreen + 100 + (icons[0].bounds.Height + iconSpacing) * 5;
                    if (!icons[index].name.Equals(RANDOM_BOBBER_INDEX))
                    {
                        icons[index].bounds.X += 8;
                        icons[index].bounds.Y += 10;
                        icons[index].bounds.Width = 48;
                        icons[index].bounds.Height = 44;
                    }
                    iconFronts[index].bounds.Y = icons[index].bounds.Y;
                    iconFronts[index].bounds.X = icons[index].bounds.X;
                }
                
                iconFronts[index].bounds.X = icons[index].bounds.X;
                iconFronts[index].bounds.Y = icons[index].bounds.Y;
                
                icons[index].myID = icons[index].name switch
                {
                    RANDOM_BOBBER_INDEX => 55,
                    LEFT_ARROW_INDEX => 56,
                    RIGHT_ARROW_INDEX => 57,
                    _ => indexInPage
                };
                icons[index].leftNeighborID = icons[index].name switch
                {
                    RANDOM_BOBBER_INDEX => 57,
                    LEFT_ARROW_INDEX => 55,
                    RIGHT_ARROW_INDEX => 56,
                    _ => indexInPage - 1
                };
                icons[index].rightNeighborID = icons[index].name switch
                {
                    RANDOM_BOBBER_INDEX => 56,
                    LEFT_ARROW_INDEX => 57,
                    RIGHT_ARROW_INDEX => 55,
                    _ => index == icons.Count - 4 ? 55 : indexInPage + 1
                };
                icons[index].downNeighborID = icons[index].name is RANDOM_BOBBER_INDEX or RIGHT_ARROW_INDEX or LEFT_ARROW_INDEX ? 0 : rowNumber == 5 || index + maxIconsPerRow >= icons.Count - 3 ? 55 : indexInPage + maxIconsPerRow;
                icons[index].upNeighborID = icons[index].name switch
                {
                    LEFT_ARROW_INDEX => 0,
                    RIGHT_ARROW_INDEX => 0,
                    _ => indexInPage - maxIconsPerRow
                };

                indexInPage++;
            }

            if (rowNumber != 5) y += icons[0].bounds.Height + iconSpacing;
            else
            {
                y -= 4 * (icons[0].bounds.Height + iconSpacing);
                indexInPage = 0;
            }
        }
        
        atMinPage = pageNumber == 0;
        atMaxPage = icons.Count <= (pageNumber + 1) * maxIconsPerRow * maxRows;

        initialize(xPositionOnScreen, yPositionOnScreen, width, height, showUpperRightCloseButton: true);
        if (Game1.options.SnappyMenus)
        {
            populateClickableComponentList();
            if (currentlySnappedComponent is null) currentlySnappedComponent = getComponentWithID(0);
            snapCursorToCurrentSnappedComponent();
        }
    }

    public override void performHoverAction(int x, int y)
    {
        base.performHoverAction(x, y);
        for (int i = 0; i < icons.Count; i++)
        {
            ClickableTextureComponent c = icons[i];
            iconFronts[i].sourceRect = iconFronts[i].startingSourceRect;
            if (c.containsPoint(x, y))
            {
                c.sourceRect.Location = ICON_BACK_HIGHLIGHT_POSITION;
                iconFronts[i].sourceRect.Location = new Point(iconFronts[i].sourceRect.Location.X,
                    iconFronts[i].sourceRect.Location.Y);
            }
            else c.sourceRect = ICON_BACK_RECTANGLE;
        }
    }

    public override void receiveLeftClick(int x, int y, bool playSound = true)
    {
        base.receiveLeftClick(x, y, playSound);
        int maxIconsPerRow = width / ICON_WIDTH;
        int maxRows = 5;
        int maxIcons = maxIconsPerRow * maxRows;
        int startIndex = pageNumber * maxIconsPerRow * maxRows;
        
        for (int i = 0; i < icons.Count; i++)
        {
            ClickableTextureComponent c = icons[i];
            if (!c.containsPoint(x, y)) continue;
            if (i < startIndex || i >= startIndex + maxIcons && !icons[i].name.Equals(RANDOM_BOBBER_INDEX) &&
                !icons[i].name.Equals(LEFT_ARROW_INDEX) && !icons[i].name.Equals(RIGHT_ARROW_INDEX))
            {
                continue;
            }

            if (iconFronts[i].name.Contains("ghosted"))
            {
                Game1.playSound("smallSelect");
                break;
            }
            
            switch (c.name)
            {
                case LEFT_ARROW_INDEX when !atMinPage:
                    pageNumber = Math.Max(0, pageNumber - 1);
                    setUpIcons();
                    Game1.playSound("shwip");
                    return;
                case RIGHT_ARROW_INDEX when !atMaxPage:
                    pageNumber++;
                    setUpIcons();
                    Game1.playSound("shwip");
                    return;
                case LEFT_ARROW_INDEX or RIGHT_ARROW_INDEX:
                    return;
            }

            int selection = Convert.ToInt32(c.name);
            if (Game1.player.bobberStyle.Value == selection) continue;

            Game1.playSound("button1");
            Game1.player.bobberStyle.Value = Convert.ToInt32(c.name);
            selected = Game1.player.bobberStyle.Value;
            Game1.player.usingRandomizedBobber = selected == -2;
        }
    }

    public override void draw(SpriteBatch b)
    {
        b.Draw(Game1.fadeToBlackRect, new Rectangle(0, 0, Game1.uiViewport.Width, Game1.uiViewport.Height),
            Color.Black * 0.7f);
        base.draw(b);
        SpriteText.drawStringWithScrollCenteredAt(b, Game1.content.LoadString("Strings\\1_6_Strings:ChooseBobber"),
            xPositionOnScreen + width / 2, yPositionOnScreen + 20, "",
            1f, Game1.textColor);
        
        int maxIconsPerRow = width / ICON_WIDTH;
        int maxRows = 5;
        int maxIcons = maxIconsPerRow * maxRows;
        int startIndex = pageNumber * maxIconsPerRow * maxRows;
        
        for (int i = 0; i < icons.Count; i++)
        {
            if (i < startIndex || i >= startIndex + maxIcons && !icons[i].name.Equals(RANDOM_BOBBER_INDEX) && !icons[i].name.Equals(LEFT_ARROW_INDEX) && !icons[i].name.Equals(RIGHT_ARROW_INDEX))
                continue;
            
            if (selected == i)
            {
                Rectangle rect = icons[i].bounds;
                rect.Inflate(2, 4);
                rect.X += ICON_X_OFFSET - 2;
                b.Draw(Game1.staminaRect, rect, Color.Red);
                if (icons[i].sourceRect.Width > 0)
                {
                    icons[i].sourceRect.X = ICON_BACK_HIGHLIGHT_POSITION.X;
                    icons[i].sourceRect.Y = ICON_BACK_HIGHLIGHT_POSITION.Y;
                }
            }
            else if (selected == -2 && icons[i].name.Equals(RANDOM_BOBBER_INDEX))
            {
                b.Draw(Game1.mouseCursors_1_6, icons[i].getVector2(), new Rectangle(480, 28, 16, 16), Color.Red, 0f,
                    Vector2.Zero, 4f, SpriteEffects.None, 1f);
            }
            
            icons[i].draw(b, Color.White, 0f, 0, ICON_X_OFFSET);
            if (iconFronts[i].name.Equals("ghosted_fade"))
            {
                iconFronts[i].draw(b, Color.Black * 0.4f, 0.87f, 0, ICON_X_OFFSET);
            } else if ((icons[i].name.Equals(LEFT_ARROW_INDEX) && atMinPage) ||
                       (icons[i].name.Equals(RIGHT_ARROW_INDEX) && atMaxPage))
            {
                iconFronts[i].draw(b, Color.White * 0.4f, 0.87f, 0, ICON_X_OFFSET);
            } else iconFronts[i].draw(b, Color.White, 0.87f, 0, ICON_X_OFFSET);
        }

        drawMouse(b);
    }
}