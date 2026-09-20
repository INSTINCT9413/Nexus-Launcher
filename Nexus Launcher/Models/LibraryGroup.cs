using System;
using System.Collections.Generic;

namespace Nexus_Launcher.Models
{
    /// <summary>
    /// A user created group that lives inside a single launcher,
    /// for example "Co-op" underneath Steam.
    ///
    /// Games are referenced by their library key rather than by
    /// object reference so the grouping survives a rescan.
    /// </summary>
    public class LibraryGroup
    {
        public string Id { get; set; }

        public string Launcher { get; set; }

        public string Name { get; set; }

        /// <summary>
        /// A DevExpress SVG resource key, for example
        /// "svgimages/icon%20builder/actions_flag.svg". SVG glyphs
        /// recolour themselves to the active skin.
        /// </summary>
        public string IconKey { get; set; }

        /// <summary>
        /// An image the user picked off disk. Takes priority over
        /// IconKey when both are set.
        /// </summary>
        public string IconPath { get; set; }

        public int SortOrder { get; set; }

        public List<string> Games { get; set; }

        public LibraryGroup()
        {
            Id = Guid.NewGuid().ToString("N");

            Games = new List<string>();
        }
    }
}
