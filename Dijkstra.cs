using System;
using System.Collections.Generic;
using System.Linq;
using System;
using System.Text;
using System.Threading.Tasks;

namespace Dijkstra
{
    static class Program
    {
        internal class Node
        {
            // Paths from the node
            internal List<Path> paths;
            internal List<Path> backtrackPaths;

            // Working variables
            internal bool isPermanent = false;
            internal int currentShortestPath;

            // Final values
            internal int finalShortestPath;
            internal int orderOfBecoming;

            internal string name;

            internal Node(List<Path> paths, string name)
            {
                this.paths = paths;
                backtrackPaths = new List<Path>();
                this.name = name;
                this.currentShortestPath = -1;
            }

            internal Node()
            {

            }

            internal void setPermanent()
            {
                isPermanent = true;
                finalShortestPath = currentShortestPath;
                analysableNodes.Remove(this);
            }
        }
        internal class Path
        {
            internal Node targetNode;
            internal float weight;

            internal Path(Node targetNode, float weight)
            {
                this.targetNode = targetNode;
                this.weight = weight;
            }
        }

        static List<Node> nodes;
        static List<Node> analysableNodes = new List<Node>();

        /*
         * The data is formatted in the form A B C D E F
         *                                 A -,2,3,4,5,6
         *                                 B -,-,9,10,11,12
         *                                 C
         *                                 D
         *                                 E
         *                                 F
         * where "-" indicates no path between nodes and Column -> Row = Node1 to Node2 but NOT vice versa
         */
        static public string main(string firstNodeName, string endNodeName, string filePath)
        {
            createNodesFromFile(filePath);

            // Get the start node
            int indexOfFirstNode;
            indexOfFirstNode = getNodeIndex(firstNodeName);

            // Get the end node
            int indexOfEndNode;
            indexOfEndNode = getNodeIndex(endNodeName);

            // Establish the first node as permanent
            nodes[indexOfFirstNode].currentShortestPath = 0;
            nodes[indexOfFirstNode].setPermanent();
            Node mostRecentPermanent = nodes[indexOfFirstNode];

            // Start the iteration
            while (!nodes[indexOfEndNode].isPermanent)
            {
                // Check each path from the most recently made permanent node
                foreach (Path path in mostRecentPermanent.paths)
                {
                    // If its permanent, ignore it
                    if (!path.targetNode.isPermanent)
                    {
                        // In the case that it hasn't been analysed yet
                        if (path.targetNode.currentShortestPath == -1)
                        {
                            // The shortest path so far is the most recent permanent node's shortest path + the weight of the path
                            path.targetNode.currentShortestPath = mostRecentPermanent.finalShortestPath + (int)path.weight;
                            analysableNodes.Add(path.targetNode); // Because it has now had a path calculate
                        }
                        // In case it already has a possible path
                        else
                        {
                            // Finds the larger one
                            if (path.targetNode.currentShortestPath > mostRecentPermanent.finalShortestPath + (int)path.weight)
                            {
                                path.targetNode.currentShortestPath = mostRecentPermanent.finalShortestPath + (int)path.weight;
                            }
                        }
                    }
                }

                // Now finds the shortest path (to become the new permanent one)
                Node nodeWithShortestPath = analysableNodes[0];
                foreach (Node node in analysableNodes)
                {
                    if (node.currentShortestPath < nodeWithShortestPath.currentShortestPath)
                    {
                        nodeWithShortestPath = node;
                    }
                }

                // Make the one with the shortest path permanent
                nodeWithShortestPath.setPermanent();
                mostRecentPermanent = nodeWithShortestPath;
            }

            // This means the end node is permanent
            string shortestPath = "";

            // Start going back through the network
            Node backTrackNode = nodes[indexOfEndNode];
            // Finding the shortest path route in writing
            shortestPath += backTrackNode.name;

            // While the backTrackNode isn't the first node
            while (backTrackNode != nodes[indexOfFirstNode])
            {
                Node previousNode = new Node();
                foreach (Path path in backTrackNode.backtrackPaths)
                {
                    // If the difference between the path lengths is the same as the weight of the path then it is the route
                    if (path.targetNode.finalShortestPath == backTrackNode.finalShortestPath - path.weight)
                    {
                        previousNode = path.targetNode;
                        break;
                    }
                }
                backTrackNode = previousNode;
                shortestPath += $"-{backTrackNode.name}";
            }

            // Reverse the route to show the route front to back
            char[] pathArray = shortestPath.ToCharArray(); 
            Array.Reverse(pathArray);
            shortestPath = new string(pathArray);
            shortestPath += $" ({nodes[indexOfEndNode].finalShortestPath})";
            // Print out the shortest path
            return shortestPath;
        }

        static int getNodeIndex(string nodeName)
        {
            for (int i = 0; i < nodes.Count; ++i)
            {
                if (nodes[i].name == nodeName)
                {
                    return i;
                }
            }
            return -1;
        }   

        static void createNodesFromFile(String filePath)
        {
            string[,] paths;

            string[] lines = System.IO.File.ReadAllLines(filePath);
            paths = new string[lines.Length, lines.Length];
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];
                string[] weights = line.Split(',');

                for (int j = 0; j < weights.Length; j++)
                {
                    paths[i, j] = weights[j];
                }
            }

            nodes = new List<Node>();
            for (int i = 0; i < lines.Length; ++i)
            {
                nodes.Add(new Node(new List<Path>(), ((char)(65 + i)).ToString()));
            }

            for (int i = 0; i < lines.Length; ++i)
            {
                for (int j = 0; j < lines.Length; ++j)
                {
                    if (paths[i, j] != "-")
                    {
                        nodes[i].paths.Add(new Path(nodes[j], float.Parse(paths[i, j])));
                        nodes[j].backtrackPaths.Add(new Path(nodes[i], float.Parse(paths[i, j])));
                    }
                }
            }
        }
    }
}
