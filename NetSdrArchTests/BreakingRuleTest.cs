using NetArchTest.Rules;
using NUnit.Framework;

namespace NetSdrArchTests
{
    /// <summary>
    /// INTENTIONALLY FAILING TEST — demonstrates red CI (rule violation).
    /// This test will be removed in the next commit (green CI).
    /// Rule: pretend NetSdrClientApp depends on a non-existent "UI" layer — always fails.
    /// </summary>
    public class BreakingRuleTest
    {
        [Test]
        public void Demo_BreakingRule_ApplicationShouldNotDependOnNetworking_INTENTIONALLY_FAILS()
        {
            // This rule is intentionally wrong:
            // NetSdrClientApp DOES depend on Networking (by design),
            // so this assertion will always fail — demonstrating a red build.
            var result = Types
                .InAssembly(typeof(NetSdrClientApp.Networking.ITcpClient).Assembly)
                .That()
                .ResideInNamespace("NetSdrClientApp")
                .And()
                .AreClasses()
                .ShouldNot()
                .HaveDependencyOn("NetSdrClientApp.Networking")
                .GetResult();

            Assert.That(result.IsSuccessful, Is.True,
                "[INTENTIONAL VIOLATION] This test is meant to fail. " +
                "It proves the architecture rule system works: " +
                "NetSdrClientApp correctly depends on Networking layer. " +
                "Failing types: " + string.Join(", ", result.FailingTypeNames ?? []));
        }
    }
}
