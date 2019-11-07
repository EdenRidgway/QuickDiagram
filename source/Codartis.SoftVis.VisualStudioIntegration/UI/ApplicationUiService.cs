﻿using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Media.Imaging;
using Codartis.SoftVis.Diagramming.Definition;
using Codartis.SoftVis.UI.Wpf;
using Codartis.SoftVis.UI.Wpf.View;
using Codartis.SoftVis.UI.Wpf.ViewModel;
using Codartis.Util;
using Codartis.Util.UI.Wpf.Dialogs;
using JetBrains.Annotations;

namespace Codartis.SoftVis.VisualStudioIntegration.UI
{
    /// <summary>
    /// Provides diagram UI services. Bundles the diagram control and its view model together.
    /// </summary>
    internal sealed class ApplicationUiService : WpfDiagramUiService, IApplicationUiService
    {
        private const string DialogTitle = "Quick Diagram Tool";
        private const double ExportedImageMargin = 10;

        private readonly IHostUiServices _hostUiServices;

        public Dpi ImageExportDpi { get; set; }

        public ApplicationUiService(
            IHostUiServices hostUiServices,
            [NotNull] IDiagramService diagramService,
            [NotNull] Func<IDiagramService, DiagramViewModel> diagramViewModelFactory,
            [NotNull] Func<DiagramControl> diagramControlFactory)
            : base(diagramService, diagramViewModelFactory, diagramControlFactory)
        {
            _hostUiServices = hostUiServices;
        }

        private RoslynDiagramViewModel RoslynDiagramViewModel => (RoslynDiagramViewModel)DiagramViewModel;

        public Task ShowDiagramWindowAsync() => _hostUiServices.ShowDiagramWindowAsync();

        public void ShowMessageBox(string message) => System.Windows.MessageBox.Show(message, DialogTitle);

        public void ShowPopupMessage(string message, TimeSpan hideAfter = default) => RoslynDiagramViewModel.ShowPopupMessage(message, hideAfter);

        public string SelectSaveFilename(string title, string filter)
        {
            var saveFileDialog = new SaveFileDialog { Title = title, Filter = filter };
            saveFileDialog.ShowDialog();
            return saveFileDialog.FileName;
        }

        public void ExpandAllNodes() => RoslynDiagramViewModel.ExpandAllNodes();
        public void CollapseAllNodes() => RoslynDiagramViewModel.CollapseAllNodes();

        public async Task<ProgressDialog> CreateProgressDialogAsync(string text, int maxProgress = 0)
        {
            var hostMainWindow = await _hostUiServices.GetMainWindowAsync();
            return new ProgressDialog(hostMainWindow, DialogTitle, text, maxProgress);
        }

        public async Task<BitmapSource> CreateDiagramImageAsync(
            CancellationToken cancellationToken = default,
            IIncrementalProgress progress = null,
            IProgress<int> maxProgress = null)
        {
            try
            {
                return await CreateDiagramImageAsync(
                    ImageExportDpi.Value,
                    ExportedImageMargin,
                    cancellationToken,
                    progress,
                    maxProgress);
            }
            catch (OutOfMemoryException)
            {
                HandleOutOfMemory();
                return null;
            }
        }

        private void HandleOutOfMemory()
        {
            ShowMessageBox("Cannot generate the image because it is too large. Please select a smaller DPI value.");
        }
    }
}