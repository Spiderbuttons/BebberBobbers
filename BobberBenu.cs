using System;
using System.Collections.Generic;
using System.Linq;
using BebberBobbers.Helpers;
using Force.DeepCloner;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
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
    
    private const int RANDOM_BOBBER_ID = -2;

    private const int UP_ARROW_ID = -3;
    private const int DOWN_ARROW_ID = -4;

    private int MAX_ICONS_PER_ROW = 11;
    private int MAX_ROWS = 5;
    private int MAX_ICONS_PER_PAGE = 55;
    private int START_INDEX = 0;

    public readonly List<ClickableTextureComponent> icons = [];

    [SkipForClickableAggregation]
    public readonly List<ClickableTextureComponent> iconFronts = [];

    public ClickableTextureComponent UpArrow;

    public ClickableTextureComponent DownArrow;

    private readonly int ICON_X_OFFSET = ICON_WIDTH / 2 - ICON_BACK_RECTANGLE.Width * 4 / 2 - 4;

    private int selected = -1;

    private int pageNumber = 0;

    private bool atMinPage = true;
    private bool atMaxPage = true;

    private string titleText;

    public BobberBenu()
    {
        titleText = Game1.content.LoadString("Strings\\1_6_Strings:ChooseBobber");
        setUpIcons();
    }

    public override void gameWindowSizeChanged(Rectangle oldBounds, Rectangle newBounds)
    {
        base.gameWindowSizeChanged(oldBounds, newBounds);
        setUpIcons();
    }
    
    public override void customSnapBehavior(int direction, int oldRegion, int oldID)
    {
        if (direction is 3)
        {
            setCurrentlySnappedComponentTo(UP_ARROW_ID);
        }
        
        if (direction is 2 && !atMaxPage)
        {
            int iconInRow = oldID % MAX_ICONS_PER_ROW;
            int iconID = Math.Min(iconInRow, icons.Count % MAX_ICONS_PER_ROW - 1);
            NextPage();
            setCurrentlySnappedComponentTo(iconID);
            Game1.playSound("shwip");
        }

        if (direction is 1)
        {
            int startIndex = pageNumber * MAX_ICONS_PER_ROW * MAX_ROWS;
            int iconsOnThisPage = Math.Min(icons.Count - startIndex, MAX_ICONS_PER_ROW * MAX_ROWS);
            int closestLowerMultiple = iconsOnThisPage - 1;
            while (closestLowerMultiple % MAX_ICONS_PER_ROW != 0 && closestLowerMultiple >= 0)
            {
                closestLowerMultiple--;
            }
            setCurrentlySnappedComponentTo(closestLowerMultiple);
        }
        
        if (direction is 0 && !atMinPage)
        {
            int iconInRow = oldID % MAX_ICONS_PER_ROW;
            int iconID = iconInRow + MAX_ICONS_PER_ROW * (MAX_ROWS - 1);
            PreviousPage();
            setCurrentlySnappedComponentTo(iconID);
            Game1.playSound("shwip");
        }
    }

    public override void receiveGamePadButton(Buttons button)
    {
        base.receiveGamePadButton(button);
        switch (button)
        {
            case Buttons.LeftShoulder or Buttons.LeftTrigger when !atMinPage:
                PreviousPage();
                setCurrentlySnappedComponentTo(0);
                snapCursorToCurrentSnappedComponent();
                return;
            case Buttons.RightShoulder or Buttons.RightTrigger when !atMaxPage:
                NextPage();
                setCurrentlySnappedComponentTo(0);
                snapCursorToCurrentSnappedComponent();
                return;
            case Buttons.RightShoulder or Buttons.RightTrigger or Buttons.LeftShoulder or Buttons.LeftTrigger:
                Game1.playSound("smallSelect", 800);
                return;
        }
    }
    
    public override void receiveScrollWheelAction(int direction)
    {
        switch (direction)
        {
            case > 0:
                PreviousPage(fromScroll: true);
                break;
            case < 0:
                NextPage(fromScroll: true);
                break;
        }
    }

    private void NextPage(bool fromScroll = false)
    {
        if (atMaxPage)
        {
            if (!fromScroll) Game1.playSound("smallSelect", 800);
            return;
        }
        pageNumber++;
        setUpIcons();
        currentlySnappedComponent = getComponentWithID(DOWN_ARROW_ID);
        if (!fromScroll) snapCursorToCurrentSnappedComponent();
        Game1.playSound("shwip");
    }

    private void PreviousPage(bool fromScroll = false)
    {
        if (atMinPage)
        {
            if (!fromScroll) Game1.playSound("smallSelect", 800);
            return;
        }
        pageNumber = Math.Max(0, pageNumber - 1);
        setUpIcons();
        currentlySnappedComponent = getComponentWithID(UP_ARROW_ID);
        if (!fromScroll) snapCursorToCurrentSnappedComponent();
        Game1.playSound("shwip");
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

        MAX_ICONS_PER_ROW = width / ICON_WIDTH;
        MAX_ICONS_PER_PAGE = MAX_ICONS_PER_ROW * MAX_ROWS;

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
        
        icons.Insert(0, new ClickableTextureComponent(new Rectangle(0, 0, 64, 64), null, new Rectangle(0, 0, 0, 0),
            4f, drawShadow: true)
        {
            name = RANDOM_BOBBER_ID.ToString(),
        });
        iconFronts.Insert(0, new ClickableTextureComponent(new Rectangle(0, 0, 10, 10), Game1.mouseCursors_1_6,
            new Rectangle(496, 28, 16, 16), 4f, drawShadow: true));

        selected = Game1.player.bobberStyle.Value;

        height += (icons.Count * ICON_WIDTH / width + 1) * (icons[0].bounds.Height + iconSpacing);
        yPositionOnScreen = Game1.uiViewport.Height / 2 - height / 2;
        int y = yPositionOnScreen + 100;
        START_INDEX = pageNumber * MAX_ICONS_PER_ROW * MAX_ROWS;
        int indexInPage = 0;
        for (int iconIndex = START_INDEX; iconIndex < icons.Count; iconIndex += MAX_ICONS_PER_ROW)
        {
            int rowNumber = iconIndex / MAX_ICONS_PER_ROW % MAX_ROWS;
            int rowCount = Math.Min(icons.Count - iconIndex, MAX_ICONS_PER_ROW);
            int x = xPositionOnScreen + width / 2 - MAX_ICONS_PER_ROW * ICON_WIDTH / 2;
            for (int iconInRow = 0; iconInRow < rowCount; iconInRow++)
            {
                int index = iconInRow + iconIndex;
                icons[index].bounds.X = x + iconInRow * ICON_WIDTH;
                icons[index].bounds.Y = y;
                icons[index].bounds.Width = ICON_WIDTH;
                
                iconFronts[index].bounds.X = icons[index].bounds.X;
                iconFronts[index].bounds.Y = icons[index].bounds.Y;
                
                icons[index].myID = indexInPage;
                icons[index].leftNeighborID = indexInPage - 1;
                if (iconInRow == 0)
                {
                    if (rowNumber == 0)
                        icons[index].leftNeighborID = -7777;
                    else if (rowNumber == MAX_ROWS - 1)
                        icons[index].leftNeighborID = DOWN_ARROW_ID;
                    else if (iconIndex + MAX_ICONS_PER_ROW >= icons.Count)
                        icons[index].leftNeighborID = -7777;
                    else icons[index].leftNeighborID = -999;
                }
                
                icons[index].rightNeighborID = iconInRow == MAX_ICONS_PER_ROW - 1 ? -999 : indexInPage + 1;
                icons[index].downNeighborID = rowNumber == MAX_ROWS - 1 || index + MAX_ICONS_PER_ROW >= icons.Count ? -7777 : indexInPage + MAX_ICONS_PER_ROW;
                icons[index].upNeighborID = rowNumber != 0 ? indexInPage - MAX_ICONS_PER_ROW : -7777;

                indexInPage++;
            }

            if (rowNumber != MAX_ROWS - 1) y += icons[0].bounds.Height + iconSpacing;
            else
            {
                y -= 4 * (icons[0].bounds.Height + iconSpacing);
                indexInPage = 0;
            }
        }

        if (icons[0].name == RANDOM_BOBBER_ID.ToString())
        {
            iconFronts[0].bounds.X += 2;
            iconFronts[0].bounds.Y += 1;
        }
        
        atMinPage = pageNumber == 0;
        atMaxPage = icons.Count <= (pageNumber + 1) * MAX_ICONS_PER_ROW * MAX_ROWS;
        
        int leftX = (xPositionOnScreen + width / 2 - MAX_ICONS_PER_ROW * ICON_WIDTH / 2) - ICON_WIDTH + 10;
        int leftY = yPositionOnScreen + 100 + 11;
        UpArrow = new ClickableTextureComponent(
            new Rectangle(leftX, leftY, 48, 48), Game1.mouseCursors,
            new Rectangle(421, 459, 11, 12), 4f)
        {
            name = UP_ARROW_ID.ToString(),
            myID = UP_ARROW_ID,
            downNeighborID = DOWN_ARROW_ID,
            upNeighborID = -999,
            leftNeighborID = -999,
            rightNeighborID = 0,
        };
        
        DownArrow = new ClickableTextureComponent(
            new Rectangle(leftX, leftY + ICON_WIDTH * (MAX_ROWS - 1), 48, 48), Game1.mouseCursors,
            new Rectangle(421, 472, 11, 12), 4f)
        {
            name = DOWN_ARROW_ID.ToString(),
            myID = DOWN_ARROW_ID,
            upNeighborID = UP_ARROW_ID,
            downNeighborID = -999,
            leftNeighborID = -999,
        };
        
        int iconsOnThisPage = Math.Min(icons.Count - START_INDEX, MAX_ICONS_PER_ROW * MAX_ROWS);
        int closestLowerMultiple = iconsOnThisPage - 1;
        while (closestLowerMultiple % MAX_ICONS_PER_ROW != 0 && closestLowerMultiple >= 0)
        {
            closestLowerMultiple--;
        }
        // DownArrow.rightNeighborID = Math.Max(closestLowerMultiple, 0);
        DownArrow.rightNeighborID = -7777;

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
        
        if (UpArrow.containsPoint(x, y))
        {
            PreviousPage();
            return;
        }
        
        if (DownArrow.containsPoint(x, y))
        {
            NextPage();
            return;
        }
        
        for (int i = 0; i < icons.Count; i++)
        {
            ClickableTextureComponent c = icons[i];
            if (!c.containsPoint(x, y)) continue;
            if (i < startIndex || i >= startIndex + maxIcons && !icons[i].name.Equals(RANDOM_BOBBER_ID.ToString()) &&
                !icons[i].name.Equals(UP_ARROW_ID.ToString()) && !icons[i].name.Equals(DOWN_ARROW_ID.ToString()))
            {
                continue;
            }

            if (iconFronts[i].name.Contains("ghosted"))
            {
                Game1.playSound("smallSelect");
                break;
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
        SpriteText.drawStringWithScrollCenteredAt(b, titleText,
            xPositionOnScreen + width / 2, yPositionOnScreen + 20, "",
            1f, Game1.textColor);

        UpArrow.draw(b, atMinPage ? Color.White * 0.4f : Color.White, 0.87f, 0, ICON_X_OFFSET);
        DownArrow.draw(b, atMaxPage ? Color.White * 0.4f : Color.White, 0.87f, 0, ICON_X_OFFSET);
        
        for (int i = 0; i < icons.Count; i++)
        {
            if (i < START_INDEX || i >= START_INDEX + MAX_ICONS_PER_PAGE)
                continue;
            
            if (selected == i - 1)
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
            else if (selected == -2 && icons[i].name.Equals(RANDOM_BOBBER_ID.ToString()))
            {
                b.Draw(Game1.mouseCursors_1_6, icons[i].getVector2(), new Rectangle(480, 28, 16, 16), Color.Red, 0f,
                    Vector2.Zero, 4f, SpriteEffects.None, 1f);
            }
            
            icons[i].draw(b, Color.White, 0f, 0, ICON_X_OFFSET);
            if (iconFronts[i].name.Equals("ghosted_fade"))
            {
                iconFronts[i].draw(b, Color.Black * 0.4f, 0.87f, 0, ICON_X_OFFSET);
            } else iconFronts[i].draw(b, Color.White, 0.87f, 0, ICON_X_OFFSET);
        }

        drawMouse(b);
    }
}