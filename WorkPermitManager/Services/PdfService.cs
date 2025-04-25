using WorkPermitManager.Models;

namespace WorkPermitManager.Services
{
    public interface IPdfService
    {
        /// <summary>
        /// Generates a PDF report based on the provided report type and data.
        /// </summary>
        /// <param name="reportType">The type of report to generate (e.g., "Summary", "Detailed").</param>
        /// <param name="workers">A list of ServiceWorker objects to include in the report.</param>
        /// <param name="outputStream">The stream where the generated PDF will be written.</param>
        /// <returns>A Task that resolves to true if the report was successfully generated, otherwise false.</returns>
        Task<bool> GenerateReportAsync(string reportType, List<ServiceWorker> workers, Stream outputStream);
    }
}