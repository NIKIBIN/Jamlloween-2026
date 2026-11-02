using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Spike2.Core.Tests
{
    /// <summary>EditMode tests of the wave 0 contracts (pure logic, no scene).</summary>
    public class CoreTests
    {
        struct Ping { public int n; }

        [SetUp]
        public void SetUp()
        {
            Bus.Clear();
            Services.Reset();
            MapRegistry.Clear();
            MapData.Clear();
            DebugRegistry.Clear();
            TunablesHub.Clear();
        }

        [Test]
        public void Bus_DeliversToSubscribers_AndSurvivesThrowingHandler()
        {
            int got = 0;
            System.Action<Ping> bad = _ => throw new System.Exception("boom");
            System.Action<Ping> good = p => got += p.n;
            Bus.Subscribe(bad);
            Bus.Subscribe(good);
            UnityEngine.TestTools.LogAssert.Expect(LogType.Exception, new System.Text.RegularExpressions.Regex("boom"));
            Bus.Publish(new Ping { n = 3 });
            Assert.AreEqual(3, got);
            Bus.Unsubscribe(good);
            Bus.Unsubscribe(bad);
            Bus.Publish(new Ping { n = 3 });
            Assert.AreEqual(3, got);
        }

        [Test]
        public void Bus_AnySeesEveryEvent()
        {
            object last = null;
            Bus.Any += e => last = e;
            Bus.Publish(new MachineDone { machineId = "M3", layoutId = "M3" });
            Assert.IsInstanceOf<MachineDone>(last);
        }

        [Test]
        public void Services_StubsAnswerForEveryContract()
        {
            Assert.IsNotNull(Services.Get<IPlayer>());
            Assert.IsNotNull(Services.Get<IPocket>());
            Assert.IsNotNull(Services.Get<IThrowables>());
            Assert.IsNotNull(Services.Get<IClipboard>());
            Assert.IsNotNull(Services.Get<IPanelSystem>());
            Assert.IsNotNull(Services.Get<IZatan>());
            Assert.IsNotNull(Services.Get<IZatanDirector>());
            Assert.IsNotNull(Services.Get<ICameraNetwork>());
            Assert.IsNotNull(Services.Get<IAnnouncer>());
            Assert.IsNotNull(Services.Get<IAmbushWarning>());
            Assert.IsNotNull(Services.Get<ICaptureDirector>());
            Assert.IsNotNull(MapRegistry.Resolve<IDoor>("d_gerador"));
            Assert.IsNotNull(MapRegistry.Resolve<IMachine>("M3"));
            Assert.IsFalse(Services.Has<IZatan>());
        }

        [Test]
        public void Services_RealImplementationWinsOverStub()
        {
            var pocket = new Stubs.NullPocket();
            Services.Register<IPocket>(pocket);
            Assert.AreSame(pocket, Services.Get<IPocket>());
            Assert.IsTrue(Services.Has<IPocket>());
            Services.Unregister<IPocket>(pocket);
            Assert.AreNotSame(pocket, Services.Get<IPocket>());
        }

        [Test]
        public void StubPocket_And_StubMachine_Work()
        {
            var pocket = Services.Get<IPocket>();
            pocket.Give("chave_deposito");
            Assert.IsTrue(pocket.Has("chave_deposito"));
            Assert.IsTrue(pocket.Take("chave_deposito"));
            Assert.IsFalse(pocket.Has("chave_deposito"));

            string done = null;
            Bus.Subscribe<MachineDone>(e => done = e.machineId);
            var m = Services.Get<IPanelSystem>().Get("M3");
            Assert.AreSame(m, Services.Get<IPanelSystem>().Get("M3"));
            m.ForceComplete(publish: false);
            Assert.IsTrue(m.IsDone);
            Assert.IsNull(done);
            m.ResetAll();
            m.CompleteCurrentStep();
            Assert.AreEqual("M3", done);
        }

        [Test]
        public void MapRegistry_FindsByIdAndKind_AndFlagsDuplicates()
        {
            var a = new GameObject("a"); var b = new GameObject("b"); var c = new GameObject("c");
            try
            {
                MapId.Add(a, "M3", "machine");
                MapId.Add(b, "M4", "machine");
                Assert.AreEqual(a, MapRegistry.GameObject("M3"));
                Assert.AreEqual(2, MapRegistry.All("machine").Count);
                Assert.AreEqual(0, MapRegistry.All("door").Count);
                UnityEngine.TestTools.LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("duplicate id 'M3'"));
                MapId.Add(c, "M3", "machine");
                CollectionAssert.Contains(MapRegistry.Duplicates, "M3");
            }
            finally { Object.DestroyImmediate(a); Object.DestroyImmediate(b); Object.DestroyImmediate(c); }
        }

        [Test]
        public void MapId_ParsesEntryData()
        {
            var go = new GameObject("cam");
            try
            {
                var m = MapId.Add(go, "cam_sala", "camera", Json.Serialize(new Dictionary<string, object> { { "id", "cam_sala" }, { "boot_order", 1.0 }, { "height", 2.6 } }));
                Assert.AreEqual(1, JsonData.Int(m.Data, "boot_order"));
                Assert.AreEqual(2.6f, m.Float("height"), 1e-4f);
            }
            finally { Object.DestroyImmediate(go); }
        }

        [Test]
        public void Json_RoundTrip()
        {
            var src = new Dictionary<string, object> { { "s", "a\"b\\c\nd ♪" }, { "n", 1.5 }, { "b", true }, { "l", new List<object> { 1.0, "x", null } } };
            var back = (Dictionary<string, object>)Json.Parse(Json.Serialize(src));
            Assert.AreEqual("a\"b\\c\nd ♪", back["s"]);
            Assert.AreEqual(1.5, back["n"]);
            Assert.AreEqual(true, back["b"]);
            Assert.AreEqual(3, ((List<object>)back["l"]).Count);
        }

        [Test]
        public void MapData_RealMap_ZonesAndActZones()
        {
            var json = AssetDatabase.LoadAssetAtPath<TextAsset>(EditorTools.Spike2Setup.MapJsonPath);
            Assert.IsNotNull(json, "circus_map_v2.json missing");
            MapData.Load(json.text);
            Assert.AreEqual(12, MapData.Zones.Count);
            Assert.AreEqual("praca", MapData.ZoneAt(new Vector3(0, 0, -30)));
            Assert.AreEqual("tenda", MapData.ZoneAt(new Vector3(0, 0, 10)));
            Assert.AreEqual("carrossel", MapData.ZoneAt(new Vector3(36, 0, 2)));
            Assert.AreEqual("anel", MapData.ZoneAt(new Vector3(0, 0, -8)));
            CollectionAssert.Contains(MapData.ActAnchors(2), "w_ger_patio");
            Assert.Greater(MapData.ActPatrol(2).Count, 0);
            Assert.IsTrue(JsonData.Bool(MapData.Circuit("trailer"), "on_at_start"));
            Assert.AreEqual(152, MapData.Section("blocks").Count);
        }

        class TestTunables : TunablesBase
        {
            [UnityEngine.Range(0f, 10f)] public float speed = 3f;
        }

        [Test]
        public void TunablesHub_HandsOutCopies()
        {
            var a = TunablesHub.Get<TestTunables>();
            a.speed = 9f;
            Assert.AreSame(a, TunablesHub.Get<TestTunables>());
            TunablesHub.ResetToDefaults(a);
            Assert.AreEqual(3f, a.speed);
            Assert.AreEqual("Test", a.Tab);
        }

        [Test]
        public void DebugRegistry_OwnerlessAndOwnedEntries()
        {
            bool flag = false;
            var owner = new GameObject("owner");
            DebugRegistry.AddToggle("Zatan", "x", () => flag, v => flag = v);
            DebugRegistry.AddAction("Painel", "y", () => { }, owner);
            CollectionAssert.AreEquivalent(new[] { "Zatan", "Painel" }, DebugRegistry.Tabs);
            Object.DestroyImmediate(owner);
            CollectionAssert.AreEquivalent(new[] { "Zatan" }, DebugRegistry.Tabs);
        }
    }
}
