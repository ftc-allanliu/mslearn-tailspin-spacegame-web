using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using TailSpin.SpaceGame.Web.Models;

namespace TailSpin.SpaceGame.Web.Controllers
{
    public class HomeController : Controller
    {
        // High score repository.
        private readonly IDocumentDBRepository<Score> _scoreRepository;
        // User profile repository.
        private readonly IDocumentDBRepository<Profile> _profileRepository;
        // Logger instance.
        private readonly ILogger<HomeController> _logger;

        public HomeController(
            IDocumentDBRepository<Score> scoreRepository,
            IDocumentDBRepository<Profile> profileRepository,
            ILogger<HomeController> logger
            )
        {
            _scoreRepository = scoreRepository;
            _profileRepository = profileRepository;
            _logger = logger;
        }

        public async Task<IActionResult> Index(
            int page = 1, 
            int pageSize = 10, 
            string mode = "",
            string region = ""
            )
        {
            // Sanitize and validate pagination parameters
            page = Math.Max(1, page);
            pageSize = (pageSize < 1 || pageSize > 100) ? 10 : pageSize;

            // Create the view model with initial values we already know.
            var vm = new LeaderboardViewModel
            {
                Page = page,
                PageSize = pageSize,
                SelectedMode = mode,
                SelectedRegion = region,
                Scores = Enumerable.Empty<ScoreProfile>(),

                GameModes = new List<string>()
                {
                    "Solo",
                    "Duo",
                    "Trio"
                },

                GameRegions = new List<string>()
                {
                    "Milky Way",
                    "Andromeda",
                    "Pinwheel",
                    "NGC 1300",
                    "Messier 82",
                }
            };

            try
            {
                // Form the query predicate.
                // Select all scores that match the provided game mode and region (map).
                // Select the score if the game mode or region is empty.
                Func<Score, bool> queryPredicate = score =>
                    (string.IsNullOrEmpty(mode) || score.GameMode == mode) &&
                    (string.IsNullOrEmpty(region) || score.GameRegion == region);

                // Fetch the total number of results in the background.
                var countItemsTask = _scoreRepository.CountItemsAsync(queryPredicate);

                // Fetch the scores that match the current filter.
                IEnumerable<Score> scores = await _scoreRepository.GetItemsAsync(
                    queryPredicate, // the predicate defined above
                    score => score.HighScore, // sort descending by high score
                    page - 1, // subtract 1 to make the query 0-based
                    pageSize
                  );

                // Wait for the total count.
                vm.TotalResults = await countItemsTask;

                // Set previous and next hyperlinks.
                if (page > 1)
                {
                    vm.PrevLink = $"/?page={page - 1}&pageSize={pageSize}&mode={Uri.EscapeDataString(mode ?? "")}&region={Uri.EscapeDataString(region ?? "")}#leaderboard";
                }
                if (vm.TotalResults > page * pageSize)
                {
                    vm.NextLink = $"/?page={page + 1}&pageSize={pageSize}&mode={Uri.EscapeDataString(mode ?? "")}&region={Uri.EscapeDataString(region ?? "")}#leaderboard";
                }

                // Fetch the user profile for each score asynchronously without blocking thread pool threads.
                var profileTasks = scores.Select(score => _profileRepository.GetItemAsync(score.ProfileId));
                var profiles = await Task.WhenAll(profileTasks);

                // Combine each score with its profile.
                vm.Scores = scores.Zip(profiles, (score, profile) => new ScoreProfile { Score = score, Profile = profile });

                return View(vm);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while loading the leaderboard.");
                return View(vm);
            }
        }

        [Route("/profile/{id}")]
        public async Task<IActionResult> Profile(string id, string rank="")
        {
            try
            {
                // Fetch the user profile with the given identifier.
                var profile = await _profileRepository.GetItemAsync(id);
                if (profile == null)
                {
                    return NotFound();
                }

                return View(new ProfileViewModel { Profile = profile, Rank = rank });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while retrieving profile for ID: {ProfileId}", id);
                return RedirectToAction(nameof(Index));
            }
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
