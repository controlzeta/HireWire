using HireWire.Application.Interfaces;
using HireWire.Domain.Entities;
using HireWire.Domain.Enums;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace HireWire.Web.Services;

/// <summary>
/// Periodic background service to check communications and flag job leads that have been stagnant for more than 4 days.
/// </summary>
public class FollowUpWorker(
    IServiceScopeFactory scopeFactory,
    ILogger<FollowUpWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("FollowUpWorker background service has started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                logger.LogInformation("FollowUpWorker starting periodic check cycle for stagnant leads...");

                using (var scope = scopeFactory.CreateScope())
                {
                    var repository = scope.ServiceProvider.GetRequiredService<IJobLeadRepository>();
                    var leads = await repository.GetAllAsync();
                    
                    var contactadoLeads = leads.Where(l => l.Status == JobLeadStatus.Contactado).ToList();
                    
                    foreach (var lead in contactadoLeads)
                    {
                        var fullLead = await repository.GetByIdAsync(lead.Id);
                        if (fullLead == null || fullLead.Communications == null || !fullLead.Communications.Any())
                        {
                            continue;
                        }

                        // Get the most recent communication sent
                        var lastComm = fullLead.Communications.OrderByDescending(c => c.SentDate).First();
                        
                        // Flag if sent date is older than 4 days and no response or follow up is active
                        if (lastComm.SentDate < DateTime.UtcNow.AddDays(-4) && !lastComm.RequiresFollowUp)
                        {
                            logger.LogInformation("JobLead {JobLeadId} ({JobTitle}) has been stagnant since {SentDate}. Marking for follow-up.", 
                                fullLead.Id, fullLead.JobTitle, lastComm.SentDate);
                            
                            lastComm.SetFollowUp(DateTime.UtcNow);
                        }
                    }

                    await repository.SaveChangesAsync();
                }

                logger.LogInformation("FollowUpWorker check cycle completed successfully.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred during the FollowUpWorker check execution.");
            }

            // Sleep for 12 hours
            await Task.Delay(TimeSpan.FromHours(12), stoppingToken);
        }

        logger.LogInformation("FollowUpWorker background service is stopping.");
    }
}
