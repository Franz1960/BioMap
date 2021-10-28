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
  [System.Diagnostics.DebuggerDisplay("{ToString()}")]
  public class TreeNode
  {
    public TreeNode(ITreeNodeData data) {
      this.Data = data;
    }
    public override string ToString() {
      return "TreeNode(" + this.Data?.InvariantName + ") + " + this.Children.Count+ " children";
    }
    public TreeNode Clone() {
      var clone = new TreeNode(this.Data) {
        Children = this.Children
      };
      return clone;
    }
    public ITreeNodeData Data { get; private set; }
    public TreeNode Parent = null;
    public IReadOnlyList<TreeNode> Children { get; private set; } = new List<TreeNode>();
    public bool HasChildren => this.Children.Count() >= 1;
    public void Clear() {
      ((List<TreeNode>)this.Children).Clear();
    }
    public void Add(TreeNode childNode) {
      ((List<TreeNode>)this.Children).Add(childNode);
    }
    public bool Remove(TreeNode childNode) {
      return ((List<TreeNode>)this.Children).Remove(childNode);
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
    public TreeNode[] Find(string sInvariantName) {
      var lResult = new List<TreeNode>();
      if (sInvariantName != null) {
          if (this.Data?.InvariantName == sInvariantName) {
          lResult.Add(this);
        }
        foreach (var child in this.Children) {
          var result = child.Find(sInvariantName);
          lResult.AddRange(result);
        }
      }
      return lResult.ToArray();
    }
    public TreeNode FindFirst(string sInvariantName) {
      var aResult = this.Find(sInvariantName);
      if (aResult.Length >= 1) {
        return aResult[0];
      } else {
        return null;
      }
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
