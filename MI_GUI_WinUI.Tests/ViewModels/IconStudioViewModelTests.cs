using Microsoft.VisualStudio.TestTools.UnitTesting;
using MI_GUI_WinUI.ViewModels;
using MI_GUI_WinUI.Tests.TestUtils;
using MI_GUI_WinUI.Services.Interfaces;
using Moq;
using System.Collections.ObjectModel;
using Microsoft.Extensions.Logging;

namespace MI_GUI_WinUI.Tests.ViewModels
{
    [TestClass]
    public class IconStudioViewModelTests : UnitTestBase
    {
        private IconStudioViewModel _viewModel;

        [TestInitialize]
        public override async Task InitializeTest()
        {
            await base.InitializeTest();

            // Create the ViewModel with mock services
            _viewModel = new IconStudioViewModel(
                MockStableDiffusionService.Object,
                Mock.Of<ILogger<IconStudioViewModel>>(),
                MockNavigationService.Object
            );
        }

        [TestMethod]
        public void InitialState_IsCorrect()
        {
            // Assert
            Assert.IsFalse(_viewModel.IsGenerating);
            Assert.IsFalse(_viewModel.IsImageGenerated);
            Assert.IsTrue(string.IsNullOrEmpty(_viewModel.InputDescription));
            Assert.AreEqual(1, _viewModel.NumberOfImages);
            Assert.IsTrue(_viewModel.UseGpu);
        }
    }
}