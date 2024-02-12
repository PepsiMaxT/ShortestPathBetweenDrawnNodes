using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using static ShortestPathBetweenDrawnNodes.Game1;

namespace ShortestPathBetweenDrawnNodes
{
    public partial class Game1 : Game
    {
        internal class TextBox
        {
            const int paddingHorizontal = 5;
            const int paddingVertical = 5;
            const int lineSpace = 2;

            internal Rectangle rectangle;
            protected Texture2D texture;
            internal SpriteFont font;

            internal string[] text;

            public TextBox()
            {

            }

            public TextBox(SpriteFont font, Rectangle rectangle, Texture2D texture, string text)
            {
                this.font = font;
                this.rectangle = rectangle;
                this.texture = texture;

                string[] wordsInText = text.Split(' ');
                this.text = splitIntoLines(wordsInText);
            }

            private string[] splitIntoLines(string[] words)
            {
                List<string> lines = new List<string>();

                int currentLineLength = 0;
                StringBuilder currentLine = new StringBuilder();
                int maxLineLength = rectangle.Width - (2 * paddingHorizontal);
                for (int i = 0; i < words.Length; i++)
                {
                    // Check if the new string fits in the current line
                    if (font.MeasureString(words[i]).X + currentLineLength < maxLineLength)
                    {
                        // Add the word to the current line
                        currentLine.Append(words[i] + " ");
                        currentLineLength = (int)font.MeasureString(currentLine).X;
                    }
                    else
                    {
                        // Remove the extra space at the end
                        currentLine.Length--;
                        // Reset the current line
                        lines.Add(currentLine.ToString());
                        currentLine.Clear();
                        currentLine.Append(words[i] + " ");
                        currentLineLength = (int)font.MeasureString(currentLine).X;
                        if (currentLineLength > maxLineLength)
                        {
                            throw new Exception($"The line does not fit in the rectangle, line length: {currentLineLength}, max length: {maxLineLength}");
                        }
                    }
                }
                if (currentLine.Length > 0)
                {
                    currentLine.Length--;
                    lines.Add(currentLine.ToString());
                }

                // Check that the lines fit in the rectangle
                if (paddingVertical + (font.MeasureString("A").Y + lineSpace) * lines.Count - lineSpace > rectangle.Height - (2 * paddingVertical))
                {
                    throw new Exception("The text does not fit in the rectangle");
                }

                return lines.ToArray();
            }

            internal virtual void Draw()
            {
                spriteBatch.Draw(texture, rectangle, null, Color.White, 0f, Vector2.Zero, SpriteEffects.None, 0.2f);

                int charHeight = (int)font.MeasureString("A").Y;
                int verticalInset = paddingVertical + (int)(((rectangle.Height - 2 * paddingVertical) - (charHeight + lineSpace) * text.Length - lineSpace) / 2f);
                int lineCount = 0;
                foreach (string line in text)
                {
                    int inset = paddingHorizontal + (int)(((rectangle.Width - 2 * paddingHorizontal) - font.MeasureString(line).X) / 2f);
                    spriteBatch.DrawString(font, line, new Vector2(rectangle.X + inset, rectangle.Y + verticalInset + (charHeight + lineSpace) * lineCount), Color.Black, 0f, Vector2.Zero, 1f, SpriteEffects.None, 1f);
                    lineCount++;
                }
            }

            internal virtual void Draw(Color color)
            {
                spriteBatch.Draw(texture, rectangle, null, color, 0f, Vector2.Zero, SpriteEffects.None, 0.2f);

                int charHeight = (int)font.MeasureString("A").Y;
                int verticalInset = paddingVertical + (int)(((rectangle.Height - 2 * paddingVertical) - (charHeight + lineSpace) * text.Length - lineSpace) / 2f);
                int lineCount = 0;
                foreach (string line in text)
                {
                    int inset = paddingHorizontal + (int)(((rectangle.Width - 2 * paddingHorizontal) - font.MeasureString(line).X) / 2f);
                    spriteBatch.DrawString(font, line, new Vector2(rectangle.X + inset, rectangle.Y + verticalInset + (charHeight + lineSpace) * lineCount), Color.Black, 0f, Vector2.Zero, 1f, SpriteEffects.None, 1f);
                    lineCount++;
                }
            }
        }

        internal class Button : TextBox
        {
            internal delegate void SetButtonOverlay(Button sender);
            internal delegate void OnClick(Object sender);
            internal OnClick onClick;
            internal delegate void Hover(Button sender);
            internal Hover defaultHover = (Button sender) =>
            {
                spriteBatch.Draw(sender.texture, sender.rectangle, null, Color.LightGray * 0.5f, 0f, Vector2.Zero, SpriteEffects.None, 0.6f);
            };
            internal Hover onHover;
            internal Hover checkHover;
            internal bool isHovered = false;

            internal Button() : base()
            {

            }

            internal Button(SpriteFont font, string text, Rectangle rectangle, Texture2D texture, OnClick onClick, Hover onHover, Hover checkHover)
                : base(font, rectangle, texture, text)
            {
                this.checkHover += checkHover;
                if (onHover == null) this.onHover = defaultHover;
                else this.onHover = onHover;
                this.onClick += onClick;
            }

            internal Button(SpriteFont font, string text, Rectangle rectangle, Texture2D texture, OnClick onClick)
                : base(font, rectangle, texture, text)
            {
                checkHover += (Button sender) => 
                {
                    sender.isHovered = sender.rectangle.Contains(mouse.Position);
                };
                onHover = defaultHover;
                this.onClick += onClick;
            }

            internal void Update()
            {
                checkHover(this);
                
                if (isHovered && mouse.LeftButton == ButtonState.Pressed && !mouseDownLastFrameLeft)
                {
                    onClick(this);
                }
            }

            internal override void Draw()
            {
                base.Draw();

                if (isHovered)
                {
                    onHover(this);
                }
            }

            internal override void Draw(Color color)
            {
                base.Draw(color);

                if (isHovered)
                {
                    onHover(this);
                }
            }
        }

        internal class NodeInputBox : TextBox
        {
            internal delegate void SetValue(NodeInputBox sender);
            SetValue setValue;
            internal bool selecting = false;

            int scrollValue = 0;

            List<Button> buttons = new List<Button>();

            public NodeInputBox(SpriteFont font, Rectangle rectangle, Texture2D texture, SetValue setter) : base(font, rectangle, texture, "")
            {
                setValue = setter;
            }

            public void update()
            {
                if (mouse.LeftButton == ButtonState.Pressed && !mouseDownLastFrameLeft)
                {
                    Rectangle clickableRectangle = new Rectangle(rectangle.X, rectangle.Y, rectangle.Width, rectangle.Height + (buttons.Count - scrollValue) * ((int)font.MeasureString("A").Y + 20));
                    if (clickableRectangle.Contains(mouse.Position))
                    {   
                        selecting = true;
                        for (int i = 0; i < nodes.Count; i++)
                        {
                            buttons.Add(new Button(font, nodes[i].text[0], new Rectangle(rectangle.X, rectangle.Y + rectangle.Height + i * ((int)font.MeasureString("A").Y + 20), rectangle.Width, (int)font.MeasureString("A").Y + 20), texture, (Object sender) =>
                            {
                                this.text[0] = ((Button)(sender)).text[0];
                                setValue(this);
                                selecting = false;
                                scrollValue = 0;
                                buttons.Clear();
                            }));
                        }
                    }
                    else
                    {
                        selecting = false;
                        scrollValue = 0;
                        buttons.Clear();
                    }
                }

                if (mouse.RightButton == ButtonState.Pressed)
                {
                    selecting = false;
                    scrollValue = 0;
                    buttons.Clear();
                }

                if (selecting)
                {
                    checkScroll();

                    for (int i = scrollValue; i < buttons.Count; i++)
                    {
                        buttons[i].Update();
                        if (buttons.Count == 0) break;
                    }
                }
            }

            void checkScroll()
            {
                if (mouse.ScrollWheelValue < mouseScrollWheelLastFrame)
                {
                    scrollValue++;
                    if (scrollValue > Math.Max(buttons.Count - 5, 0)) scrollValue = Math.Max(buttons.Count - 5, 0);
                    else
                    {
                        foreach (Button b in buttons)
                        {
                            b.rectangle.Offset(0, -((int)font.MeasureString("A").Y + 20));
                        }
                    }
                }
                else if (mouse.ScrollWheelValue > mouseScrollWheelLastFrame)
                {
                    scrollValue--;
                    if (scrollValue < 0) scrollValue = 0;
                    else
                    {
                        foreach (Button b in buttons)
                        {
                            b.rectangle.Offset(0, (int)font.MeasureString("A").Y + 20);
                        }
                    }
                }
            }

            internal override void Draw()
            {
                base.Draw();

                if (selecting)
                {
                    for (int i = scrollValue; i < buttons.Count; i++) 
                    {
                        buttons[i].Draw();
                    }
                }
            }
        }
    }
}
