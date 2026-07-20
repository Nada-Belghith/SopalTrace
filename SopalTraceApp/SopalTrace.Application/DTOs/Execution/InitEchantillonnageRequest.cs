using System.Collections.Generic;

namespace SopalTrace.Application.DTOs.Execution
{
    public class InitEchantillonnageRequest
    {
        public string? PosteCode { get; set; }
        public int? NbPostes { get; set; }
        public List<string> InstrumentCodes { get; set; } = new List<string>();
        public string? MachineCode { get; set; }
        public string? Equipe { get; set; }
        public string? MatriculeOperateur { get; set; }
    }
}
