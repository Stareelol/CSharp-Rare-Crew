namespace Csharp_Rare_Crew.Models
{
    public class Employee
    {
        // Since we get a JSON from the API, we need 3 fields + one for the computation of the totalTime
        public required string EmployeeName { get; set; }
        public required DateTime StarTimeUtc { get; set; }
        public required DateTime EndTimeUtc { get; set; }
        public double TotalTime { get; set; }
    }
}
