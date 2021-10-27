using System;
using System.Collections.Generic;
using System.Linq;

namespace BioMap
{
  /// <summary>
  /// A data element for tree nodes.
  /// </summary>
  public interface ITreeNodeData
  {
    string InvariantName { get; }
    /// <summary>
    /// Common name in given language.
    /// </summary>
    /// <param name="sLanguageName">
    /// The language name, 'de' or 'en'.
    /// </param>
    /// <returns>
    /// The common name in the given language; the invariant name if no common name is found.
    /// </returns>
    string GetLocalizedName(string sLanguageName);
  }
}
