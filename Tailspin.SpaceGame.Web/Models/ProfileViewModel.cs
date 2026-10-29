namespace TailSpin.SpaceGame.Web.Models
{
    public class ProfileViewModel
    {
        // The player profile.
        public Profile Profile { get; set; }
        // The player's rank according to the active filter.
        public string Rank { get; set; }
    }
}