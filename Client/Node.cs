﻿using System;
using System.Collections.Generic;
using System.Data.Common;
using Opc.Ua;
using Opc.Ua.Client;

namespace Client
{
    public class Node
    {
        public ReferenceDescription Description { get; set; }
        public ExpandedNodeId ParentId { get; set; }
        public List<Node> Children { get; set; }
        
        public Node(){}
        
        public Node(ReferenceDescription referenceDescription, ExpandedNodeId parentId)
        {
            Children = new List<Node>();
            Description = referenceDescription;
            ParentId = parentId;
        }
        

        public static List<Node> OrderList(List<Node> unorderedList)
        {
            List<Node> orderedList = new List<Node>();
            List<Node> handledNodes = new List<Node>(); // To keep track of processed nodes.
            
            

            foreach (Node node in unorderedList)
            {
                if (node.ParentId == null) // Node must be root -> then every other nodes are children.
                {
                    orderedList.Add(node);
                    handledNodes.Add(node);
                }
                
                BuildTree(unorderedList, ref handledNodes, node);               
            }
            
            return orderedList;
        }

        private static void BuildTree(List<Node> unorderedList, ref List<Node> handledNodes, Node parent, bool debug = false)
        {
            
            foreach (Node node in unorderedList)
            {
                if (node.ParentId != null) // If node is not root. Necessary as root nodes still exist in the unordered list.
                {
                    if (node.ParentId.Identifier == parent.Description.NodeId.Identifier)
                    {                    
                        if (handledNodes.Contains(node) == false)
                        {
                            parent.Children.Add(node);
                            handledNodes.Add(node);
                    
                            if (debug) // For debug. Will print all relationships in console. 
                            {
                                Console.WriteLine("Added " + node.Description.DisplayName + " to " + parent.Description.DisplayName);
                            }
                
                            BuildTree(unorderedList, ref handledNodes, node, debug);
                        }   
                    }    
                }
            }
        }
        
        public static List<Node> Discover(Session session)
        {
            ReferenceDescriptionCollection rootNameSpaces = BrowseRoot(session);
            List<Node> nodes = new List<Node>();
            
            foreach (var root in rootNameSpaces)
            {
                nodes.Add(new Node(root, null)); // Root can not have parent, thereby parentId is null.
            }
                 
            foreach (ReferenceDescription reference in rootNameSpaces)
            {
                NamespaceWalk(session, reference, nodes);
            }
            return nodes;
        }   
        
        private static List<Node> NamespaceWalk(Session session, ReferenceDescription node, List<Node> nodes)
        {            
            ReferenceDescriptionCollection nextRefs;
            Byte[] nextCp;
            session.Browse(
                null,
                null,
                ExpandedNodeId.ToNodeId(node.NodeId, session.NamespaceUris),
                0u,
                BrowseDirection.Forward,
                ReferenceTypeIds.HierarchicalReferences,
                true,
                (uint)NodeClass.Variable | (uint)NodeClass.Object | (uint)NodeClass.Method,
                out nextCp,
                out nextRefs);
            
            foreach (ReferenceDescription reference in nextRefs)
            {
                nodes.Add(new Node(reference, node.NodeId));
            }
                   
            foreach (ReferenceDescription reference in nextRefs)
            {
                NamespaceWalk(session, reference, nodes);
            }
            
            return nodes;
        }
        
        private static ReferenceDescriptionCollection BrowseRoot(Session session)
        {
            ReferenceDescriptionCollection references;
            Byte[] continuationPoint;

            references = session.FetchReferences(ObjectIds.ObjectsFolder);

            session.Browse(
                null,
                null,
                ObjectIds.ObjectsFolder,
                0u,
                BrowseDirection.Forward,
                ReferenceTypeIds.HierarchicalReferences,
                true,
                (uint)NodeClass.Variable | (uint)NodeClass.Object | (uint)NodeClass.Method,
                out continuationPoint,
                out references);

            return references;
        }
    }
}