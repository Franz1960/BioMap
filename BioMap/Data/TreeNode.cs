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
  public class TreeNode : IComparable
  {
    public TreeNode(ITreeNodeData data) {
      this.Data = data;
    }
    public override string ToString() {
      return "TreeNode(" + this.Data?.InvariantName + ") + " + this.Children.Count+ " children";
    }
    public int CompareTo(object obj) {
      string a = this.Data?.InvariantName;
      string b = (obj as TreeNode)?.Data?.InvariantName;
      if (a == null) {
        if (b == null) {
          return 0;
        } else {
          return -1;
        }
      } else {
        if (b == null) {
          return 1;
        } else {
          if (a.StartsWith("(")) {
            if (b.StartsWith("(")) {
              return a.Substring(1).CompareTo(b.Substring(1));
            } else {
              return 1;
            }
          } else {
            if (b.StartsWith("(")) {
              return -1;
            } else {
              return a.CompareTo(b);
            }
          }
        }
      }
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
    public bool IsCollection => (this.Parent?.Data?.InvariantName != null && this.Parent.Data.InvariantName.StartsWith("("));
    public void Clear() {
      ((List<TreeNode>)this.Children).Clear();
    }
    public void Add(TreeNode childNode) {
      var l = ((List<TreeNode>)this.Children);
      l.Add(childNode);
      l.Sort();
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
