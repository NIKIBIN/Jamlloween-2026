using NUnit.Framework;
using Spike2.Core;
using UnityEditor;
using UnityEngine;

namespace Spike2.Acts.Tests
{
    public class ActsCoreTests
    {
        [SetUp]
        public void SetUp()
        {
            Bus.Clear(); Services.Reset(); MapRegistry.Clear();
            var json = AssetDatabase.LoadAssetAtPath<TextAsset>(Core.EditorTools.Spike2Setup.MapJsonPath);
            MapData.Load(json.text);
        }

        [TearDown]
        public void TearDown() => MapData.Clear();

        [Test]
        public void TargetMachines_FromJson()
        {
            Assert.AreEqual("M1", WorldStateSetup.TargetMachine(ActId.Act0));
            Assert.AreEqual("M3", WorldStateSetup.TargetMachine(ActId.Act2));
            Assert.AreEqual("M6", WorldStateSetup.TargetMachine(ActId.Final));
            CollectionAssert.AreEquivalent(new[] { "chave_deposito", "fusivel_reserva" }, WorldStateSetup.ActItems(ActId.Act2));
        }

        [Test]
        public void For_Act4_HasEarlierMachinesDone_AndPowerOn()
        {
            var w = WorldStateSetup.For(ActId.Act4, null, null);
            CollectionAssert.AreEquivalent(new[] { "M1", "M2", "M3", "M4" }, w.machinesDone);
            Assert.IsTrue(w.zatanActive);
            Assert.IsTrue(w.camerasReport);
            Assert.IsTrue(WorldStateSetup.CircuitOn(w, "carrossel"), "on_after M3");
            Assert.IsTrue(WorldStateSetup.CircuitOn(w, "trailer"), "on_at_start");
            Assert.IsFalse(w.carouselOn);
        }

        [Test]
        public void For_Act2_GeneratorCircuitsStillOff_ZatanActive()
        {
            var w = WorldStateSetup.For(ActId.Act2, null, null);
            CollectionAssert.AreEquivalent(new[] { "M1", "M2" }, w.machinesDone);
            Assert.IsFalse(WorldStateSetup.CircuitOn(w, "gerador"));
            Assert.IsTrue(w.zatanActive);
            Assert.IsTrue(w.pickupsActive["garrafas_cabine"]);
        }

        [Test]
        public void For_Act1_ZatanDormant_Final_UnlocksCoxiaOnlyAfterFinal()
        {
            Assert.IsFalse(WorldStateSetup.For(ActId.Act1, null, null).zatanActive);
            Assert.IsFalse(WorldStateSetup.For(ActId.Final, null, null).doorsLocked.ContainsKey("d_coxia"));
            Assert.IsFalse(WorldStateSetup.For(ActId.Ended, null, null).doorsLocked["d_coxia"]);
        }

        [Test]
        public void Apply_WorksWithStubsOnly()
        {
            WorldStateSetup.Apply(WorldStateSetup.For(ActId.Act3, null, null));
            Assert.Pass();
        }

        [Test]
        public void PhaseOfActs()
        {
            Assert.AreEqual(ZatanPhase.Observe, ActDirector.PhaseOf(ActId.Act1));
            Assert.AreEqual(ZatanPhase.Hunt, ActDirector.PhaseOf(ActId.Act3));
            Assert.AreEqual(ZatanPhase.Show, ActDirector.PhaseOf(ActId.Final));
        }
    }
}
