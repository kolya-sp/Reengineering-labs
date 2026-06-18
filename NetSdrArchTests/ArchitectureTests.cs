using NetArchTest.Rules;
using NUnit.Framework;

namespace NetSdrArchTests
{
    /// <summary>
    /// Lab 5 — Architectural rules using NetArchTest
    /// These tests enforce dependency constraints between layers.
    /// </summary>
    public class ArchitectureTests
    {
        private const string AppAssembly = "NetSdrClientApp";

        // ---------------------------------------------------------------
        // Rule 1: Networking layer must NOT depend on Messages layer
        // Rationale: networking is infrastructure, it should not know about
        //            domain-level message construction
        // ---------------------------------------------------------------
        [Test]
        public void Networking_ShouldNotDependOn_Messages()
        {
            var result = Types
                .InAssembly(typeof(NetSdrClientApp.Networking.ITcpClient).Assembly)
                .That()
                .ResideInNamespace("NetSdrClientApp.Networking")
                .ShouldNot()
                .HaveDependencyOn("NetSdrClientApp.Messages")
                .GetResult();

            Assert.That(result.IsSuccessful, Is.True,
                "Networking layer must not depend on Messages layer. " +
                "Failing types: " + string.Join(", ", result.FailingTypeNames ?? []));
        }

        // ---------------------------------------------------------------
        // Rule 2: Messages layer must NOT depend on Networking layer
        // Rationale: message building is pure domain logic,
        //            independent of transport
        // ---------------------------------------------------------------
        [Test]
        public void Messages_ShouldNotDependOn_Networking()
        {
            var result = Types
                .InAssembly(typeof(NetSdrClientApp.Networking.ITcpClient).Assembly)
                .That()
                .ResideInNamespace("NetSdrClientApp.Messages")
                .ShouldNot()
                .HaveDependencyOn("NetSdrClientApp.Networking")
                .GetResult();

            Assert.That(result.IsSuccessful, Is.True,
                "Messages layer must not depend on Networking layer. " +
                "Failing types: " + string.Join(", ", result.FailingTypeNames ?? []));
        }

        // ---------------------------------------------------------------
        // Rule 3: All classes in Networking namespace must be
        //         interfaces OR implement an interface from the same namespace
        // Rationale: all networking components must be abstracted
        //            behind interfaces for testability
        // ---------------------------------------------------------------
        [Test]
        public void NetworkingClasses_ShouldImplementInterface()
        {
            var result = Types
                .InAssembly(typeof(NetSdrClientApp.Networking.ITcpClient).Assembly)
                .That()
                .ResideInNamespace("NetSdrClientApp.Networking")
                .And()
                .AreClasses()
                .Should()
                .ImplementInterface(typeof(NetSdrClientApp.Networking.ITcpClient))
                .Or()
                .ImplementInterface(typeof(NetSdrClientApp.Networking.IUdpClient))
                .GetResult();

            Assert.That(result.IsSuccessful, Is.True,
                "All concrete classes in Networking must implement ITcpClient or IUdpClient. " +
                "Failing types: " + string.Join(", ", result.FailingTypeNames ?? []));
        }

        // ---------------------------------------------------------------
        // Rule 4: NetSdrClient (top-level orchestrator) must NOT
        //         reside in Networking or Messages sub-namespaces —
        //         it belongs to the root application namespace only
        // ---------------------------------------------------------------
        [Test]
        public void NetSdrClient_ShouldResideIn_RootNamespace()
        {
            var result = Types
                .InAssembly(typeof(NetSdrClientApp.Networking.ITcpClient).Assembly)
                .That()
                .HaveNameStartingWith("NetSdrClient")
                .And()
                .AreClasses()
                .Should()
                .ResideInNamespace("NetSdrClientApp")
                .GetResult();

            Assert.That(result.IsSuccessful, Is.True,
                "NetSdrClient class must reside in root NetSdrClientApp namespace. " +
                "Failing types: " + string.Join(", ", result.FailingTypeNames ?? []));
        }
    }
}
