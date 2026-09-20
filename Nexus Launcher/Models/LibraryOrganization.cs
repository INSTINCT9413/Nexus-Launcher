using System.Collections.Generic;

namespace Nexus_Launcher.Models
{
    /// <summary>
    /// Everything the user has arranged by hand: which games are
    /// favourited and which user created groups exist inside each
    /// launcher. Serialized to LibraryOrganization.json.
    /// </summary>
    public class LibraryOrganization
    {
        public List<string> Favorites { get; set; }

        public List<LibraryGroup> Groups { get; set; }

        public LibraryOrganization()
        {
            Favorites = new List<string>();

            Groups = new List<LibraryGroup>();
        }
    }
}
