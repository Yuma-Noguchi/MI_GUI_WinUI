using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MI_GUI_WinUI.Services.Interfaces;
using Moq;
using Moq.Language;
using Moq.Language.Flow;
using System.Linq.Expressions;

namespace MI_GUI_WinUI.Tests.TestUtils
{
    /// <summary>
    /// Base class for unit tests providing mock services
    /// </summary>
    public abstract class UnitTestBase : TestBase
    {
        // Common mock services that most tests will need
        protected Mock<INavigationService> MockNavigationService { get; private set; }
        protected Mock<IActionService> MockActionService { get; private set; }
        protected Mock<IStableDiffusionService> MockStableDiffusionService { get; private set; }
        protected Mock<ILogger> MockLogger { get; private set; }

        /// <summary>
        /// Configures services with mocked dependencies
        /// </summary>
        protected override void ConfigureServices(IServiceCollection services)
        {
            base.ConfigureServices(services);

            // Create mock services
            MockNavigationService = TestHelpers.CreateMockNavigationService();
            MockActionService = TestHelpers.CreateMockActionService();
            MockStableDiffusionService = TestHelpers.CreateMockStableDiffusionService();
            MockLogger = new Mock<ILogger>();

            // Register mock services
            services.AddSingleton(MockNavigationService.Object);
            services.AddSingleton(MockActionService.Object);
            services.AddSingleton(MockStableDiffusionService.Object);
            services.AddSingleton(MockLogger.Object);

            // Allow derived classes to configure additional services
            ConfigureTestServices(services);
        }

        /// <summary>
        /// Can be overridden by derived classes to add additional mock services
        /// </summary>
        protected virtual void ConfigureTestServices(IServiceCollection services)
        {
        }

        /// <summary>
        /// Verifies that a service method was called exactly once
        /// </summary>
        protected void VerifyServiceCall<T>(Mock<T> mock, Action<T> verifyExpression) where T : class
        {
            mock.Verify(x => verifyExpression(It.IsAny<T>()), Times.Once);
        }

        /// <summary>
        /// Verifies that a service method was never called
        /// </summary>
        protected void VerifyServiceNotCalled<T>(Mock<T> mock, Action<T> verifyExpression) where T : class
        {
            mock.Verify(x => verifyExpression(It.IsAny<T>()), Times.Never);
        }

        /// <summary>
        /// Sets up a mock to throw an exception when a specific method is called
        /// </summary>
        protected void SetupServiceException<T, TException>(
            Mock<T> mock,
            Action<T> setupExpression,
            string message = "Test exception")
            where T : class
            where TException : Exception, new()
        {
            mock.Setup(x => setupExpression(It.IsAny<T>()))
                .Throws(new TException());
        }

        /// <summary>
        /// Sets up a mock to return a value when a specific method is called
        /// </summary>
        protected void SetupServiceReturn<T, TResult>(
            Mock<T> mock,
            Expression<Func<T, TResult>> setupExpression,
            TResult result)
            where T : class
        {
            mock.Setup(setupExpression)
                .Returns(result);
        }

        /// <summary>
        /// Sets up a mock to return a value when a specific function is called
        /// </summary>
        public void SetupFunc<T, TReturn>(Mock<T> mock, Expression<Func<T, TReturn>> expression, TReturn returnValue) where T : class
        {
            mock.Setup(expression).Returns(returnValue);
        }

        /// <summary>
        /// Resets all mock service call counts
        /// </summary>
        protected void ResetAllMocks()
        {
            MockNavigationService.Reset();
            MockActionService.Reset();
            MockStableDiffusionService.Reset();
            MockLogger.Reset();
        }

        /// <summary>
        /// Verifies all mock service expectations
        /// </summary>
        protected void VerifyAllMocks()
        {
            MockNavigationService.VerifyAll();
            MockActionService.VerifyAll();
            MockStableDiffusionService.VerifyAll();
            MockLogger.VerifyAll();
        }
    }
}