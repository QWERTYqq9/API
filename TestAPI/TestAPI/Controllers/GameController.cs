using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

public class GameController : ControllerBase
{
    private readonly HttpClient _httpClient;

    public GameController(IHttpClientFactory httpClientFactory)
    {
        _httpClient = httpClientFactory.CreateClient();
    }

    [HttpGet("genres")]
    public async Task<ActionResult<GenreInfo[]>> GetGenres()
    {
        var response = await _httpClient.GetAsync("https://store.steampowered.com/api/getgenrelist/?cc=us&l=english");
        if (response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync();
            var genres = JsonConvert.DeserializeObject<GenreResponse>(content)?.Genres;
            if (genres != null)
            {
                var genreInfos = genres.Select(g => new GenreInfo { Name = g.Name }).Take(10).ToArray();
                return Ok(genreInfos);
            }
        }

        return StatusCode((int)response.StatusCode);
    }

    private class GenreResponse
    {
        public Genre[] Genres { get; set; }
    }

    private class Genre
    {
        public string Name { get; set; }
    }

    private class Tab
    {
        public string Name { get; set; }
        public List<GameIdInfo> Items { get; set; }
    }

    [HttpGet("games/{name}")]
    public async Task<ActionResult<List<GameIdInfo>>> GetGamesByName(string name)
    {
        var url = $"https://store.steampowered.com/api/getappsingenre/?genre={name}&cc=us&l=english";
        var response = await _httpClient.GetAsync(url);
        if (response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync();
            var tabs = JsonConvert.DeserializeObject<JObject>(content)?["tabs"]?.ToObject<Dictionary<string, Tab>>();
            if (tabs != null)
            {
                var games = new List<GameIdInfo>();
                foreach (var tab in tabs.Values)
                {
                    games.AddRange(tab.Items.Take(5));
                }

                return Ok(games.Take(5).ToList());
            }
        }

        return StatusCode((int)response.StatusCode);
    }

    private class AppDetails
    {
        public GameData Data { get; set; }
    }

    private class GameData
    {
        public string Name { get; set; }
        public bool IsFree { get; set; }
        public string AboutTheGame { get; set; }
        public string HeaderImage { get; set; }
        public string Website { get; set; }
        public string[] Developers { get; set; }
        public string[] Publishers { get; set; }
    }

    [HttpGet("game/{appId}")]
    public async Task<ActionResult<GameDetails>> GetGameDetails(string appId)
    {
        var url = $"https://store.steampowered.com/api/appdetails?appids={appId}";
        var response = await _httpClient.GetAsync(url);
        if (response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync();
            var data = JsonConvert.DeserializeObject<Dictionary<string, AppDetails>>(content);
            if (data.TryGetValue(appId, out var appDetails))
            {
                var gameDetails = new GameDetails
                {
                    Name = appDetails.Data.Name,
                    IsFree = appDetails.Data.IsFree,
                    About = appDetails.Data.AboutTheGame,
                    HeaderImage = appDetails.Data.HeaderImage,
                    Website = appDetails.Data.Website,
                    Developers = appDetails.Data.Developers,
                    Publishers = appDetails.Data.Publishers
                };

                return Ok(gameDetails);
            }
        }

        return StatusCode((int)response.StatusCode);
    }
}