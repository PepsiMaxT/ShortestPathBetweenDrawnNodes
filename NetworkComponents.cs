using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace ShortestPathBetweenDrawnNodes
{
    public partial class Game1 : Game 
    {
        internal class Node : Button
        {
            // Node click explanation
            static internal bool nodeClicked = false;
            static internal Node clickedNode;
            static internal bool movingNode = false;
            static internal bool makingBiPath = false;
            static internal bool removedPath = false;

            // Paths from the node
            internal List<Path> paths = new List<Path>();

            // Click function for the button
            OnClick GetOnNodeClick()
            {
                OnClick function;
                function = (Object sender) =>
                {
                    removedPath = false;
                    // If the control key is pressed, the node is moved
                    if (keyboard.IsKeyDown(Keys.LeftControl) && !movingNode)
                    {
                        movingNode = true;
                        clickedNode = (Node)sender;
                        return;
                    }

                    // If the shift key is pressed, a bi-directional path is created
                    if (keyboard.IsKeyDown(Keys.LeftShift) && !makingBiPath)
                    {
                        makingBiPath = true;
                        Node.nodeClicked = true;
                        clickedNode = (Node)sender;
                        return;
                    }

                    // If the backspace key is pressed, the node is removed
                    if (keyboard.IsKeyDown(Keys.Back))
                    {
                        // Add it to removal nodes
                        nodesForRemoval.Add((Node)sender);
                        // Removing the mock path
                        List<MockPath> mockPathsToRemove = new List<MockPath>();
                        foreach (MockPath path in mockPaths)
                        {
                            // Find a match in mockPaths
                            if (path.node1 == (Node)sender || path.node2 == (Node)sender)
                            {
                                mockPathsToRemove.Add(path);
                            }
                        }

                        // Remove them
                        foreach (MockPath path in mockPathsToRemove)
                        {
                            mockPaths.Remove(path);
                        }
                        // Exits out of the function
                        return;
                    }

                    // Checks for a generic click for a new line
                    if (Node.nodeClicked == false)
                    {
                        Node.nodeClicked = true;
                        Node.clickedNode = (Node)sender;
                    }
                    // New line in progress and finishing
                    else
                    {
                        // Reset
                        Node.nodeClicked = false;
                        // Add the new node
                        if (Node.AddPath(clickedNode, (Node)sender, 1))
                        {
                            // in case it is new
                            MockPath mockPath = new MockPath(clickedNode, (Node)sender, 1);
                            // Check if the mockPath already exists (e.g if the path is already there in the other direction)
                            foreach (MockPath path in mockPaths)
                            {
                                if (path == mockPath)
                                {
                                    // Exits out if it is
                                    return;
                                }
                            }
                            mockPaths.Add(mockPath);
                        }

                        // Creating a bi-directional path
                        if (makingBiPath)
                        {
                            // Finding the node to make the bi path to
                            bool pathExists = false;
                            // Check if this new path makes it bi-directional anyway
                            foreach (Path path in ((Node)sender).paths)
                            {
                                if (path.targetNode == clickedNode)
                                {
                                    // If the path exists no more checks are needed
                                    pathExists = true;
                                    break;
                                }
                            }
                            // If it doesn't exist make it exist
                            if (!pathExists)
                            {
                                Node.AddPath((Node)sender, clickedNode, 1);
                            }
                            makingBiPath = false;
                        }
                    }
                };

                return function;
            }
            
            // Checks if the node is hovered by a radius from the centre
            static Hover GetNodeHoverCheck()
            {
                Hover function;
                function = (Button sender) =>
                {
                    sender.isHovered = (Math.Pow(mouse.X - sender.rectangle.Center.X, 2) + Math.Pow(mouse.Y - sender.rectangle.Center.Y, 2) < Math.Pow(sender.rectangle.Height / 2, 2)); 
                };
                return function;
            }

            // Empty constructor
            public Node() : base()
            {

            }

            // Constructor with all the parameters
            public Node(SpriteFont font, string nodeName, Rectangle rectangle, Texture2D texture) : base(font, nodeName, rectangle, texture, new Node().GetOnNodeClick(), null, GetNodeHoverCheck())
            {

            }

            // Draw the node and also the paths
            internal override void Draw()
            {
                foreach (Path path in paths)
                {
                    drawPath(path);
                }

                Color nodeColor = Color.Red;

                if (paths.Count > 0)
                {
                    foreach (MockPath mockPath in mockPaths)
                    {
                        if (mockPath.node1 == this)
                        {
                            foreach (Path path in mockPath.node2.paths)
                            {
                                if (path.targetNode == this)
                                {
                                    nodeColor = Color.White;
                                }
                            }
                        }
                        else if (mockPath.node2 == this)
                        {
                            foreach (Path path in mockPath.node1.paths)
                            {
                                if (path.targetNode == this)
                                {
                                    nodeColor = Color.White;
                                }
                            }
                        }
                    }
                }

                base.Draw(nodeColor);
            }

            // Draw the path between the nodes
            void drawPath(Path path)
            {
                // get the two points (centres of nodes)
                Vector2 point1 = rectangle.Center.ToVector2();
                Vector2 point2 = path.targetNode.rectangle.Center.ToVector2();
                // Draw a line between the two points
                Vector2 direction = point2 - point1;
                // Calculate the angle + magnitude
                float angle = (float)Math.Atan2(direction.Y, direction.X);
                float distance = Vector2.Distance(point1, point2);
                Color drawColor = Color.Black;
                
                // Highlight the path if it is in the shortest path
                if (pathInShortest(path))
                {
                    drawColor = Color.Red;
                }

                // Draw the line at the angle
                spriteBatch.Draw(pixel, point1, null, drawColor, angle, Vector2.Zero, new Vector2(distance, 2), SpriteEffects.None, 0.1f);

                // Draw an arrowHead at the end of the line at the edge of the node
                float arrowScale = 1.5f;
                // Calculate the position of the arrowHead
                Vector2 arrowHeadPosition = new Vector2(point2.X + 0.5f - (float)Math.Cos(angle) * rectangle.Width / 2f, point2.Y + 0.5f - (float)Math.Sin(angle) * rectangle.Height / 2f);
                // Centre the arrowHead to the line and at the edge of the node
                arrowHeadPosition += arrowScale * new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle)) - (arrowTexture.Height * arrowScale / 2f) * new Vector2(-(float)Math.Sin(angle), (float)Math.Cos(angle));
                // Draw the arrowhead
                spriteBatch.Draw(arrowTexture, arrowHeadPosition, null, drawColor, angle, Vector2.Zero, arrowScale, SpriteEffects.None, 0.7f);
            }

            bool pathInShortest(Path path)
            {
                // Check that a shortest path exists
                if (shortestPathFound == null) return false;
                // Remove the length of the path
                string shortestPathFoundImit = shortestPathFound.Split(' ')[0];
                // Get each individual node
                string[] pathArray = shortestPathFoundImit.Split('-');
                for (int i = 0; i < pathArray.Length - 1; i++)
                {
                    // Checks if two sequential nodes are in a path
                    if (pathArray[i] == text[0] && pathArray[i + 1] == path.targetNode.text[0])
                    {
                        return true;
                    }
                }
                return false;
            }

            // Draw the path to the mouse pointer
            static internal void drawWorkingPath()
            {
                // Works the same as DrawPath but with the mouse position
                Vector2 point1 = clickedNode.rectangle.Center.ToVector2();
                Vector2 point2 = mouse.Position.ToVector2();
                // Draw a line between the two points
                Vector2 direction = point2 - point1;
                float angle = (float)Math.Atan2(direction.Y, direction.X);
                float distance = Vector2.Distance(point1, point2);
                spriteBatch.Draw(pixel, point1, null, Color.Black, angle, Vector2.Zero, new Vector2(distance, 2), SpriteEffects.None, 0.1f);
            }

            // Add a path to the node
            static bool AddPath(Node startNode, Node targetNode, int weight)
            {
                // Cannot add a path to itself
                if (startNode == targetNode) return false;

                // Suggested path
                Path path = new Path();
                path.targetNode = targetNode;
                path.weight = weight;

                // Check if the path already exists
                foreach (Path existingPath in startNode.paths)
                {
                    if (path == existingPath)
                    {
                        // Remove the path if it is
                        removedPath = true;
                        // Find the node it links to to remove both ways
                        Node connectedNode = existingPath.targetNode;
                        startNode.paths.Remove(existingPath);
                        // Find the path in the other node
                        foreach (Path connectedPath in connectedNode.paths)
                        {
                            if (connectedPath.targetNode == startNode)
                            {
                                connectedNode.paths.Remove(connectedPath);
                                break;
                            }
                        }
                        // Remove the mockPath linked
                        foreach (MockPath mockPath in mockPaths)
                        {
                            // Find it
                            if ((mockPath.node1 == startNode && mockPath.node2 == connectedNode) || (mockPath.node2 == startNode && mockPath.node1 == connectedNode))
                            {
                                mockPaths.Remove(mockPath);
                                break;
                            }
                        }
                        // Recheck the connections
                        incompleteConnectionsFlag = !AllNodesConnected();
                        // Add path failed, removed instead
                        return false;
                    }
                }
                // Add the path as it was valid
                startNode.paths.Add(path);
                incompleteConnectionsFlag = !AllNodesConnected();
                return true;
            }
        }

        internal class Path
        {
            internal Node targetNode;
            internal int weight;

            public static bool operator ==(Path path1, Path path2)
            {
                return (path1.targetNode == path2.targetNode && path1.weight == path2.weight);
            }

            public static bool operator !=(Path path1, Path path2)
            {
                return !(path1 == path2);
            }
        }

        internal class MockPath
        {
            internal Node node1;
            internal Node node2;
            internal int weight;
            internal string weightString;

            internal Rectangle rectangle;

            internal bool editingWeight = false;

            internal MockPath(Node node1, Node node2, int weight)
            {
                this.node1 = node1;
                this.node2 = node2;
                this.weight = weight;
                weightString = weight.ToString();
            }

            public static bool operator ==(MockPath path1, MockPath path2)
            {
                return (path1.node1 == path2.node1 && path1.node2 == path2.node2) || (path1.node1 == path2.node2 && path1.node2 == path2.node1);
            }

            public static bool operator !=(MockPath path1, MockPath path2)
            {
                return !(path1 == path2);
            }

            public void updateWeight(int newWeight)
            {
                weight = newWeight;
                foreach (Path path in node1.paths)
                {
                    if (path.targetNode == node2)
                    {
                        path.weight = newWeight;
                        break;
                    }
                }
                foreach (Path path in node2.paths)
                {
                    if (path.targetNode == node1)
                    {
                        path.weight = newWeight;
                        break;
                    }
                }
            }

            public void update()
            {
                if (mouse.LeftButton == ButtonState.Pressed && !mouseDownLastFrameLeft)
                {
                    if (rectangle.Contains(mouse.Position) && !editingWeight)
                    {
                        window.TextInput += onInput;
                        editingWeight = true;
                    }
                    else if (editingWeight)
                    {
                        if (weightString.Length == 0) weightString = "0";
                        if (weightString[weightString.Length - 1] == '.')
                        {
                            weightString = weightString.Remove(weightString.Length - 1);
                        }
                        updateWeight(int.Parse(weightString));
                        window.TextInput -= onInput;
                        editingWeight = false;
                    }
                }
            }

            public void Draw()
            {
                rectangle = new Rectangle((node1.rectangle.Center.X + node2.rectangle.Center.X - 50) / 2, (node1.rectangle.Center.Y + node2.rectangle.Center.Y - 10) / 2, 50, 20);
                spriteBatch.Draw(pixel, rectangle, null, Color.White, 0f, Vector2.Zero, SpriteEffects.None, 0.8f);
                spriteBatch.DrawString(Cascadia, weightString, new Vector2(rectangle.X + 2, rectangle.Y), Color.Black, 0f, Vector2.Zero, 1f, SpriteEffects.None, 1f);
            }
           
            public void onInput(object sender, TextInputEventArgs e)
            {
                var key = e.Key;
                var character = e.Character;

                int number = character - '0';

                if (weightString.Length < 5)
                {
                    if (0 <= number && number <= 9)
                    {
                        weightString += number.ToString();
                    }
                    if (weightString.Length == 0 && key == Keys.OemMinus)
                    {
                        weightString += "-";
                    }
                }
                if (key == Keys.Back)
                {
                    if (weightString.Length > 0)
                    {
                        weightString = weightString.Remove(weightString.Length - 1);
                    }
                }
                if (key == Keys.Enter)
                {
                    if (weightString.Length == 0) weightString = "0";
                    if (weightString[weightString.Length - 1] == '.')
                    {
                        weightString = weightString.Remove(weightString.Length - 1);
                    }
                    updateWeight(int.Parse(weightString));
                    editingWeight = false;
                    window.TextInput -= onInput;
                }
            }
        }
    }
}
