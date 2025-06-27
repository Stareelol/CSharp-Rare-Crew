namespace Csharp_Rare_Crew.ViewModels
{
    public class EmployeeVm
    {
        // This ViewModel is used only for the computation of TotalTime
        public required string EmployeeName { get; set; }
        public double TotalTime { get; set; }
    }
}
