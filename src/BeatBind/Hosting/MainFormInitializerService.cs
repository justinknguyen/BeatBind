using BeatBind.Application.Services;
using BeatBind.Presentation.Forms;
using Microsoft.Extensions.Hosting;

namespace BeatBind.Hosting
{
    internal sealed class MainFormInitializerService : IHostedService
    {
        private readonly MainForm _mainForm;
        private readonly HotkeyManagementService _hotkeyManagementService;

        public MainFormInitializerService(MainForm mainForm, HotkeyManagementService hotkeyManagementService)
        {
            _mainForm = mainForm;
            _hotkeyManagementService = hotkeyManagementService;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _mainForm.SetHotkeyManagementService(_hotkeyManagementService);
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
