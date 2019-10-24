﻿using System.Collections.Generic;
using Autofac;
using Codartis.SoftVis.Diagramming.Definition;
using Codartis.SoftVis.Diagramming.Definition.Layout;
using Codartis.SoftVis.Diagramming.Implementation.Layout;
using Codartis.SoftVis.Diagramming.Implementation.Layout.DirectConnector;
using Codartis.SoftVis.Services;
using Codartis.SoftVis.Services.Plugins;
using Codartis.SoftVis.TestHostApp.Diagramming;
using Codartis.SoftVis.TestHostApp.Modeling;
using Codartis.SoftVis.TestHostApp.UI;
using Codartis.SoftVis.UI;
using Codartis.SoftVis.UI.Wpf.ViewModel;

namespace Codartis.SoftVis.TestHostApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        public MainWindow()
        {
            var container = CreateDependencyContainer();

            var viewModel = container.Resolve<MainWindowViewModel>();
            DataContext = viewModel;

            InitializeComponent();

            viewModel.OnUiInitialized(mainWindow: this, diagramStyleProvider: DiagramControl);
        }

        private static IContainer CreateDependencyContainer()
        {
            var builder = new ContainerBuilder();

            builder.RegisterType<MainWindowViewModel>();

            builder.RegisterType<VisualizationService>()
                .WithParameter(
                    new TypedParameter(
                        typeof(IEnumerable<DiagramPluginId>),
                        new[]
                        {
                            DiagramPluginId.AutoLayoutDiagramPlugin,
                            DiagramPluginId.ConnectorHandlerDiagramPlugin,
                            DiagramPluginId.ModelTrackingDiagramPlugin
                        }))
                .As<IVisualizationService>();

            builder.RegisterType<TestModelServiceFactory>().As<IModelServiceFactory>();
            builder.RegisterType<TestDiagramServiceFactory>().As<IDiagramServiceFactory>();
            builder.RegisterType<TestUiServiceFactory>().As<IUiServiceFactory>();
            builder.RegisterType<DiagramLayoutAlgorithm>().WithParameter("childrenAreaPadding", 2).As<IDiagramLayoutAlgorithm>();
            builder.RegisterType<DiagramPluginFactory>().As<IDiagramPluginFactory>();
            builder.RegisterType<TestRelatedNodeTypeProvider>().As<IRelatedNodeTypeProvider>();
            builder.RegisterType<DiagramShapeUiFactory>().As<IDiagramShapeUiFactory>();

            builder.RegisterType<TestLayoutPriorityProvider>().As<ILayoutPriorityProvider>();
            builder.RegisterType<LayoutAlgorithmSelectionStrategy>().As<ILayoutAlgorithmSelectionStrategy>();
            builder.RegisterType<DirectConnectorRoutingAlgorithm>().As<IConnectorRoutingAlgorithm>();

            return builder.Build();
        }
    }
}