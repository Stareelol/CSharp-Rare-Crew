using Csharp_Rare_Crew.Models;
using Csharp_Rare_Crew.ViewModels;
using Microsoft.AspNetCore.Mvc;
using SkiaSharp;
using System.Drawing;
using System.Text.Json;

namespace Csharp_Rare_Crew.Controllers
{
    public class EmployeeController : Controller
    {

        private readonly IHttpClientFactory _httpClientFactory;
        private readonly string _apiKey;
        public EmployeeController(IHttpClientFactory c, IConfiguration cfg)
        {
            _httpClientFactory = c;
            _apiKey = cfg["TimesheetApi:Key"] ?? throw new InvalidOperationException ("Api key is missing!");
        } 

        public async Task<IActionResult> Index()
        {
            var client = _httpClientFactory.CreateClient();
            var url = $"https://rc-vault-fap-live-1.azurewebsites.net/api/gettimeentries?code={_apiKey}"; //Api key is stored in the config
            var json = await client.GetStringAsync(url);
            // Deserialize raw entires
            var employees = JsonSerializer.Deserialize<List<Employee>>(json);
            if (employees is not null)
            {
                // Group by name, calculate the total hours and sort by descending
                var viewModel = employees?.GroupBy(e => string.IsNullOrWhiteSpace(e.EmployeeName) ? "Unknown Employee" : e.EmployeeName)
                .Select(g => new EmployeeVm { EmployeeName = g.Key, TotalTime = g.Sum(e => (e.EndTimeUtc - e.StarTimeUtc).TotalHours) })
                .OrderByDescending(x => x.TotalTime)
                .ToList();
                return View(viewModel);
            }
            // In case the api call fails or no data is in employees
            else return View();
            
        }

        [Route("chart.png")]
        public async Task<IActionResult> Chart()
        {
            var client = _httpClientFactory.CreateClient();
            var url = $"https://rc-vault-fap-live-1.azurewebsites.net/api/gettimeentries?code={_apiKey}";
            var json = await client.GetStringAsync(url);
            var employees = JsonSerializer.Deserialize<List<Employee>>(json);
            if (employees is not null)
            {
                // Again, group by name, sort by descending and calculate the total hours, this time for drawing
                var data = employees.GroupBy(e => string.IsNullOrWhiteSpace(e.EmployeeName) ? "Unknown Employee" : e.EmployeeName)
               .Select(g => new EmployeeVm { EmployeeName = g.Key, TotalTime = g.Sum(e => (e.EndTimeUtc - e.StarTimeUtc).TotalHours) })
               .OrderByDescending(x => x.TotalTime)
               .ToList();
                var total = data.Sum(d => d.TotalTime);

                // Creating the canvas
                using var img = new SKBitmap(610, 500);
                using var canvas = new SKCanvas(img);
                canvas.Clear(SKColors.White);

                float start = 0;
                var rand = new Random();

                // Variable for changing the properties of the labels
                var textPaint = new SKPaint
                {
                    IsAntialias = true,
                    Color = SKColors.Black,
                };

                var size = new SKFont().Size;
                var font = new SKFont(SKTypeface.Default, size);

                // Foreach loop for drawing the slices
                foreach (var slice in data)
                {
                    // Sweep calculates the size of each employee sweep inside the pie chart
                    float sweep = (float)slice.TotalTime / (float)total * 360f;
                    // Pct and label caluclate the raw percentage inside the label for displaying
                    float pct = (float)slice.TotalTime / (float)total * 100;
                    string label = $"{slice.EmployeeName} ({pct:0.##}%)";
                    var paint = new SKPaint
                    {
                        IsAntialias = true,
                        Color = new SKColor((byte)rand.Next(256), (byte)rand.Next(256), (byte)rand.Next(256))
                    };
                    // Draw the slices to the canvas
                    canvas.DrawArc(new SKRect(110, 110, 410, 410), start, sweep, true, paint);

                    float midAngle = start + sweep / 2f;
                    float rad = midAngle * (MathF.PI / 180f);
                    float cx = 260 + MathF.Cos(rad) * 200;
                    float cy = 260 + MathF.Sin(rad) * 200;

                    // Draw the text
                    canvas.DrawText(label, cx, cy, SKTextAlign.Center, font, textPaint);

                    start += sweep;
                }
                
                //Encode to png and return the resulting image
                using var ms = new MemoryStream();
                img.Encode(ms, SKEncodedImageFormat.Png, 100);
                return File(ms.ToArray(), "image/png");
            }
            // In case the chart data is missing
            else return View();
        }
    }
}
