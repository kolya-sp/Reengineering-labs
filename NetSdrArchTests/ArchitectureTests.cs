using NetArchTest.Rules;
using NUnit.Framework;

namespace NetSdrArchTests
{
    /// <summary>
    /// Lab 5 — Architectural rules using NetArchTest.
    /// These tests enforce dependency constraints between layers.
    ///
    /// Note on project structure:
    ///   - TcpClientWrapper, ITcpClient  → namespace NetSdrClientApp.Networking
    ///   - UdpClientWrapper, IUdpClient  → global namespace (no namespace declaration)
    /// Rules are written to reflect this actual structure.
    /// </summary>
    public class ArchitectureTests
    {
        private static readonly System.Reflection.Assembly AppAssembly =
            typeof(NetSdrClientApp.Networking.ITcpClient).Assembly;

        // ---------------------------------------------------------------
        // Rule 1: Networking namespace must NOT depend on Messages namespace
        // Rationale: networking is infrastructure — must not know about
        //            domain-level message construction
        // ---------------------------------------------------------------
        [Test]
        public void Networking_ShouldNotDependOn_Messages()
        {
            var result = Types
                .InAssembly(AppAssembly)
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
        // Rule 2: Messages namespace must NOT depend on Networking namespace
        // Rationale: message building is pure domain logic,
        //            independent of transport implementation
        // ---------------------------------------------------------------
        [Test]
        public void Messages_ShouldNotDependOn_Networking()
        {
            var result = Types
                .InAssembly(AppAssembly)
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
        // Rule 3: TcpClientWrapper must implement ITcpClient
        // Rationale: TCP networking component must be abstracted
        //            behind interface for testability and substitutability
        // Note: scoped to ITcpClient only because UdpClientWrapper/IUdpClient
        //       reside in global namespace (outside NetSdrClientApp.Networking)
        // ---------------------------------------------------------------
        [Test]
        public void TcpClientWrapper_ShouldImplement_ITcpClient()
        {
            var result = Types
                .InAssembly(AppAssembly)
                .That()
                .ResideInNamespace("NetSdrClientApp.Networking")
                .And()
                .AreClasses()
                .Should()
                .ImplementInterface(typeof(NetSdrClientApp.Networking.ITcpClient))
                .GetResult();

            Assert.That(result.IsSuccessful, Is.True,
                "All concrete classes in NetSdrClientApp.Networking must implement ITcpClient. " +
                "Failing types: " + string.Join(", ", result.FailingTypeNames ?? []));
        }

        // ---------------------------------------------------------------
        // Rule 4: NetSdrClient orchestrator must reside in root namespace
        // Rationale: the top-level client must not leak into sub-layers
        // ---------------------------------------------------------------
        [Test]
        public void NetSdrClient_ShouldResideIn_RootNamespace()
        {
            var result = Types
                .InAssembly(AppAssembly)
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
