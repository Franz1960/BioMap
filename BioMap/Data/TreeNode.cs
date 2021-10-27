using System;
using System.Collections.Generic;
using System.Linq;

namespace BioMap
{
  /// <summary>
  /// A tree node for elements of a given type.
  /// </summary>
  /// <typeparam name="T">
  /// The type.
  /// </typeparam>
  public class TreeNode
  {
    public TreeNode(ITreeNodeData data) {
      this.Data = data;
    }
    public readonly ITreeNodeData Data;
    public TreeNode Parent = null;
    public TreeNode[] Children { get; private set; } = new TreeNode[0];
    public bool HasChildren => this.Children.Count() >= 1;
    public void Clear() {
      this.Children = new TreeNode[0];
    }
    public void Add(TreeNode childNode) {
      this.Children = this.Children.Append(childNode).ToArray();
    }
    public TreeNode[] Ancestors {
      get {
        var lNodes = new List<TreeNode>();
        var node = this;
        while (true) {
          node = node.Parent;
          if (node != null && node.Data != null) {
            lNodes.Insert(0, node);
          } else {
            break;
          }
        }
        return lNodes.ToArray();
      }
    }
    public TreeNode Find(string sInvariantName) {
      if (this.Data?.InvariantName == sInvariantName) {
        return this;
      }
      foreach (var child in this.Children) {
        var result = child.Find(sInvariantName);
        if (result != null) {
          return result;
        }
      }
      return null;
    }
    public IEnumerable<TreeNode> GetChildrenFlatList() {
      var flatList = new List<TreeNode>();
      foreach (var childNode in this.Children) {
        flatList.Add(childNode);
        flatList.AddRange(childNode.GetChildrenFlatList());
      }
      return flatList;
    }
  }
}
