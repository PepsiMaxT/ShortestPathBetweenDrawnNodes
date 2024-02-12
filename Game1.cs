using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace ShortestPathBetweenDrawnNodes
{
    public partial class Game1 : Game
    {
        // Graphics
        static private GraphicsDeviceManager graphics;
        static private SpriteBatch spriteBatch;

        // Window
        static GameWindow window;

        // Default objects
        static internal Texture2D pixel; // Texture
        static internal SpriteFont Cascadia; // Font

        // Game flags
        static internal bool exitFlag = false;
        static internal bool incompleteConnectionsFlag = false;

        // Input output
        static internal MouseState mouse;
        static bool mouseDownLastFrameLeft = false;
        static bool mouseDownLastFrameRight = false;
        static int mouseScrollWheelLastFrame = 0;
        static internal KeyboardState keyboard;
        void updateInput()
        {
            mouseDownLastFrameLeft = mouse.LeftButton == ButtonState.Pressed;
            mouseDownLastFrameRight = mouse.RightButton == ButtonState.Pressed;
            mouseScrollWheelLastFrame = mouse.ScrollWheelValue;
            mouse = Mouse.GetState();
            keyboard = Keyboard.GetState();
        }

        // Network information
        static int highestNodeID = 0;
        static string shortestPathFound = "";

        // UI elements
        static List<Button> UIButtons;
        Button exitButton;
        Button findPathButton;

        // Entry nodes
        static List<NodeInputBox> entryNodes;
        NodeInputBox startNodeInput;
        NodeInputBox endNodeInput;
        string startNode = "A";
        string endNode = "B";

        // Nodes
        Texture2D nodeTexture;
        static List<Node> nodes;
        static List<Node> nodesForRemoval;

        // Paths between nodes
        static List<MockPath> mockPaths;
        static Texture2D arrowTexture;

        // Network area
        static Rectangle placeableArea;

        // Root of the folder containing the graphdata
        static string folderRoot = Directory.GetCurrentDirectory();

        // Connections to nodes
        static internal void relabelNodes()
        {
            // Loop through the labels and make sure they all go from A to Z with no gaps
            highestNodeID = 0;
            for (int i = 0; i < nodes.Count; i++)
            {
                nodes[i].text[0] = ((char)(65 + i)).ToString();
                highestNodeID++;
            }
        }
        static List<Node> accessibleNodes;
        static internal bool AllNodesConnected()
        {
            bool allConnected = true;

            for (int i = 0; i < nodes.Count; i++)
            {
                if (!AllNodesConnectedTo(nodes[i]))
                {
                    allConnected = false;
                }
            }
            return allConnected;
        }
        static internal bool AllNodesConnectedTo(Node node)
        {
            accessibleNodes = new List<Node>();
            accessibleNodes.Add(node);

            CheckConnectionsFrom(node);

            return accessibleNodes.Count == nodes.Count;
        }
        static internal void CheckConnectionsFrom(Node node)
        {
            foreach (Path path in node.paths)
            {
                if (!accessibleNodes.Contains(path.targetNode))
                {
                    accessibleNodes.Add(path.targetNode);
                    CheckConnectionsFrom(path.targetNode);
                }
            }
        }

        // Puts the network into a format Dijkstra.Cs can understand
        static internal void createPathTable(string filepath)
        {
            filepath = folderRoot + filepath;

            using (StreamWriter sw = new StreamWriter(filepath))
            {
                // Sort each of the paths
                for (int i = 0; i < nodes.Count; i++)
                {
                    nodes[i].paths.Sort((x, y) => x.targetNode.text[0].CompareTo(y.targetNode.text[0]));
                }

                for (int i = 0; i < nodes.Count; i++)
                {
                    StringBuilder line = new StringBuilder();
                    for (int j = 0; j < nodes.Count; j++)
                    {
                        bool pathFound = false;
                        foreach (Path path in nodes[i].paths)
                        {
                            if (path.targetNode == nodes[j])
                            {
                                pathFound = true;
                                line.Append($"{path.weight},");
                                break;
                            } 
                        }
                        if (!pathFound) line.Append("-,");
                    }
                    line.Length--;
                    sw.WriteLine(line);
                }
                sw.Close();
            }
        }

        public Game1()
        {
            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;

            window = this.Window;
            window.AllowUserResizing = true;
        }

        protected override void Initialize()
        {
            base.Initialize();

            if (!File.Exists(folderRoot + "\\pathTable.txt"))
            {
                File.Create(folderRoot + "\\pathTable.txt").Close();
            }

            // TODO: Add your initialization logic here
            InitializeUI();

            mockPaths = new List<MockPath>();
            placeableArea = new Rectangle(100, 0, GraphicsDevice.Viewport.Width - 100, GraphicsDevice.Viewport.Height);

        }

        void InitializeUI()
        {
            // Initialise UI buttons
            exitButton = new Button(Cascadia, "CLICK ME TO EXIT", new Rectangle(0, 0, 100, 100), pixel, (Object sender) => { exitFlag = true; });
            findPathButton = new Button(Cascadia, "Find path", new Rectangle(0, 160, 100, 100), pixel, (Object sender) => {
                if (!incompleteConnectionsFlag && nodes.Count > 1)
                {
                    // Range check
                    if ((startNode[0] - 'A' < 0 || startNode[0] - 'A' > highestNodeID) || endNode[0] - 'A' < 0 || endNode[0] - 'A' > highestNodeID - 1) return;
                    // Carry out Dijkstra's algorithm
                    createPathTable("\\pathTable.txt");
                    shortestPathFound = Dijkstra.Program.main(startNode, endNode, folderRoot + "\\pathTable.txt");
                }
            });
            
            UIButtons = new List<Button>()
            {
                exitButton,
                findPathButton
            };

            // Initialise Entry nodes
            startNodeInput = new NodeInputBox(Cascadia, new Rectangle(0, 110, 40, 45), pixel, (NodeInputBox sender) => { startNode = sender.text[0]; });
            endNodeInput = new NodeInputBox(Cascadia, new Rectangle(60, 110, 40, 45), pixel, (NodeInputBox sender) => { endNode = sender.text[0]; });

            entryNodes = new List<NodeInputBox>()
            {
                startNodeInput,
                endNodeInput
            };

            // Nodes
            nodes = new List<Node>();
            nodesForRemoval = new List<Node>();
        }

        protected override void LoadContent()
        {
            spriteBatch = new SpriteBatch(GraphicsDevice);

            // TODO: use this.Content to load your game content here
            CreatePixel(ref pixel);
            LoadFonts();
            loadTextures();
        }

        void CreatePixel(ref Texture2D pixel)
        {
            pixel = new Texture2D(GraphicsDevice, 1, 1);
            pixel.SetData(new[] { Color.White });
        }
        void LoadFonts()
        {
            Cascadia = Content.Load<SpriteFont>("CascadiaMono");
        }
        void loadTextures()
        {
            nodeTexture = Content.Load<Texture2D>("NodeTexture");
            arrowTexture = Content.Load<Texture2D>("ArrowTexture");
        }

        protected override void Update(GameTime gameTime)
        {
            // TODO: Add your update logic here
            updateInput();
            handleFlags();

            // Update each of the entry nodes
            foreach (NodeInputBox inputBox in entryNodes)
            {
                inputBox.update();
            }

            // Update the UI
            if (entryNodes[0].selecting != true && entryNodes[1].selecting != true)
            {
                foreach (Button UI in UIButtons)
                {
                    UI.Update();
                }
            }

            // Move the node
            if (Node.movingNode)
            {
                Node.clickedNode.rectangle = new Rectangle(mouse.X - 25, mouse.Y - 25, 50, 50);
                if (mouse.LeftButton == ButtonState.Pressed && !mouseDownLastFrameLeft)
                {
                    Node.movingNode = false;
                    mouseDownLastFrameLeft = true;
                }
            }

            // Update each node
            foreach (Node node in nodes)
            {
                node.Update();
            }

            bool nodesToRemove = nodesForRemoval.Count > 0;
            // Remove any nodes that want to be removed
            for (int i = 0; i < nodesForRemoval.Count; i++)
            {
                // Loop through each of the nodes and remove any paths that go to the node
                foreach (Node node in nodes)
                {
                    for (int j = 0; j < node.paths.Count; j++)
                    {
                        // Check if they match
                        if (node.paths[j].targetNode == nodesForRemoval[i])
                        {
                            node.paths.Remove(node.paths[j]);
                            j--;
                        }
                    }
                }
                // Remove the nodes themselves
                nodes.Remove(nodesForRemoval[i]);
                // Relabel nodes from A -> upwards
                relabelNodes();
            }
            // Clear the list of nodes to remove
            nodesForRemoval.Clear();
            // Check if the network is incomplete
            if (nodesToRemove && nodes.Count > 0) incompleteConnectionsFlag = !AllNodesConnected();

            // Add a new node
            if (mouse.RightButton == ButtonState.Pressed && !mouseDownLastFrameRight && placeableArea.Contains(mouse.Position))
            {
                // Check if the node is in the area and doesn't overlapp over nodes
                bool possible = true;
                Rectangle possibleLocation = new Rectangle(mouse.X - 25, mouse.Y - 25, 50, 50);
                if (possibleLocation.X < placeableArea.X) possible = false;
                foreach (Node node in nodes)
                {
                    if (node.rectangle.Intersects(possibleLocation))
                    {
                        possible = false;
                    }
                }

                // Create the new node if its possible
                if (possible)
                {
                    string name = ((char)(65 + highestNodeID)).ToString();
                    if (65 + highestNodeID <= '~')
                    nodes.Add(new Node(Cascadia, ((char)(65 + highestNodeID)).ToString(), possibleLocation, nodeTexture));
                    highestNodeID++;
                    // Signal that the network needs to be checked for completeness
                    incompleteConnectionsFlag = true;
                }
            }

            foreach (MockPath mockPath in mockPaths)
            {
                mockPath.update();
            }

            base.Update(gameTime);
        }

        void handleFlags()
        {
            if (exitFlag)
            {
                Exit();
            }
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);

            // TODO: Add your drawing code here
            spriteBatch.Begin(SpriteSortMode.FrontToBack);

            // Draw the input boxes
            foreach (NodeInputBox inputBox in entryNodes)
            {
                inputBox.Draw();
            }

            // Draw the UI buttons
            foreach (Button UI in UIButtons)
            {
                // Don't draw find path if there are no entered nodes
                if (UI == findPathButton && !(entryNodes[0].selecting != true && entryNodes[1].selecting != true)) continue;
                UI.Draw();
            }

            // Update the placable area in case of resizing and draw
            placeableArea = new Rectangle(placeableArea.X, placeableArea.Y, GraphicsDevice.Viewport.Width - placeableArea.X, GraphicsDevice.Viewport.Height - placeableArea.Y);
            spriteBatch.Draw(pixel, placeableArea, null, Color.LightGray, 0f, Vector2.Zero, SpriteEffects.None, 0f);

            // Draw the nodes
            foreach (Node node in nodes)
            {
                node.Draw();
            }
            // Draw a line to your cursor if adding a new path
            if (Node.nodeClicked) Node.drawWorkingPath();

            // Draw the weight of the path
            foreach (MockPath mockPath in mockPaths)
            {
                mockPath.Draw();
            }

            // Network information
            spriteBatch.DrawString(Cascadia, (incompleteConnectionsFlag) ? "Not all nodes are connected" : "All nodes are connected", new Vector2(300, 0), Color.Black, 0f, Vector2.Zero, 1f, SpriteEffects.None, 1f);
            spriteBatch.DrawString(Cascadia, (incompleteConnectionsFlag) ? "Not all nodes are connected" : shortestPathFound, new Vector2(300, 20), Color.Black, 0f, Vector2.Zero, 1f, SpriteEffects.None, 1f);

            spriteBatch.End();

            base.Draw(gameTime);
        }
    }
}