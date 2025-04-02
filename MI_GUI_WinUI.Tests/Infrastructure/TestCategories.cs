using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;

namespace MI_GUI_WinUI.Tests.Infrastructure
{
    public static class TestCategory
    {
        public const string Unit = "Unit";
        public const string Integration = "Integration";
        public const string Performance = "Performance";
        public const string UI = "UI";
        public const string Smoke = "Smoke";
        public const string RequiresGpu = "RequiresGpu";
        public const string LongRunning = "LongRunning";
        public const string ModifiesData = "ModifiesData";
        public const string Isolated = "Isolated";
    }

    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
    public class SmokeTestAttribute : TestCategoryBaseAttribute
    {
        public override IList<string> TestCategories => new[] { TestCategory.Smoke };
    }

    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
    public class IntegrationTestAttribute : TestCategoryBaseAttribute
    {
        public override IList<string> TestCategories => new[] { TestCategory.Integration };
    }

    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
    public class PerformanceTestAttribute : TestCategoryBaseAttribute
    {
        public override IList<string> TestCategories => new[] { TestCategory.Performance };
    }

    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
    public class UITestAttribute : TestCategoryBaseAttribute
    {
        public override IList<string> TestCategories => new[] { TestCategory.UI };
    }

    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
    public class RequiresGpuAttribute : TestCategoryBaseAttribute
    {
        public override IList<string> TestCategories => new[] { TestCategory.RequiresGpu };
    }

    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
    public class LongRunningTestAttribute : TestCategoryBaseAttribute
    {
        public override IList<string> TestCategories => new[] { TestCategory.LongRunning };
    }

    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
    public class DataDependentTestAttribute : TestCategoryBaseAttribute
    {
        private readonly string _dataDescription;

        public DataDependentTestAttribute(string dataDescription)
        {
            _dataDescription = dataDescription;
        }

        public override IList<string> TestCategories => new[] { $"DataDependent_{_dataDescription}" };
    }

    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
    public class ModifiesDataAttribute : TestCategoryBaseAttribute
    {
        public override IList<string> TestCategories => new[] { TestCategory.ModifiesData };
    }

    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
    public class RequiresPlatformAttribute : TestCategoryBaseAttribute
    {
        private readonly string _feature;

        public RequiresPlatformAttribute(string feature)
        {
            _feature = feature;
        }

        public override IList<string> TestCategories => new[] { $"RequiresPlatform_{_feature}" };
    }

    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
    public class IsolatedTestAttribute : TestCategoryBaseAttribute
    {
        public override IList<string> TestCategories => new[] { TestCategory.Isolated };
    }
}