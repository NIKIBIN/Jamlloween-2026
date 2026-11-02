using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Circus.EditorTools
{
    public static partial class CircusMapBuilder
    {
        static readonly Dictionary<string, string> TarotTexts = new Dictionary<string, string>
        {
            { "T1", "Raio, queda, a torre desaba. A luz que você trouxer vai faltar quando você mais precisar." },
            { "T2", "Pendurado pelos pés, imóvel, preso pela escolha de outro. Uma porta vai se fechar por você." },
            { "T3", "Uma porta se fecha para outra se abrir. Ele cansou de brincar." },
        };

        static string[] ResolveMachineIds(string expr)
        {
            var all = L(J["machines"]).Cast<Dictionary<string, object>>().Select(m => (string)m["id"]).ToList();
            return expr.Split(new[] { " ou ", ",", " or " }, System.StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim())
                .Select(tok => all.FirstOrDefault(id => id == tok) ?? all.FirstOrDefault(id => id.StartsWith(tok)))
                .Where(x => x != null).ToArray();
        }

        static partial void AddTarotComponent(GameObject card, Dictionary<string, object> t)
        {
            var c = card.AddComponent<TarotCard>();
            c.id = (string)t["id"];
            c.cardName = (string)t["card"];
            c.foretells = (string)t["foretells"];
            c.text = TarotTexts.TryGetValue(c.id, out var tx) ? tx : c.foretells;
            c.appearsAfterAny = ResolveMachineIds((string)t["appears_after"]);
            c.startsHunt = c.foretells.ToUpperInvariant().Contains("CACAR");
        }

        static partial void AddTriggerComponent(GameObject go, Dictionary<string, object> t)
        {
            var z = go.AddComponent<ZoneTrigger>();
            z.id = (string)t["id"];
            z.condition = (string)t["condition"];
            z.effect = (string)t["effect"];
            z.size = go.GetComponent<BoxCollider>().size;
        }

        static partial void AddZatanExtras(GameObject zatan)
        {
            var src = zatan.AddComponent<AudioSource>();
            src.playOnAwake = false;
            src.spatialBlend = 1f;
        }

        static partial void AddRadioPartial(GameObject go) => go.AddComponent<RadioMessages>();

        static partial void AddNarrativeSystemsPartial(GameObject sys)
        {
            var pd = sys.AddComponent<PhaseDirector>();
            var wp = GameObject.Find("Map/ZatanWaypoints").GetComponentsInChildren<Transform>().Where(t => t.name != "ZatanWaypoints").Cast<Object>().ToList();
            Set(pd, "appearPoints", wp);

            // extra zone: control room interior (door trap foretold by "O Enforcado")
            var triggers = GameObject.Find("Map/Triggers").transform;
            var room = RoomsById()["control_room"];
            var c = V2(room["center"]); var s = V2(room["inner_size"]);
            var ev = new GameObject("EV_control_room");
            ev.transform.SetParent(triggers, false);
            ev.transform.position = new Vector3(c.x, 1.5f, c.y);
            var bc = ev.AddComponent<BoxCollider>(); bc.isTrigger = true; bc.size = new Vector3(s.x, 3f, s.y);
            var zt = ev.AddComponent<ZoneTrigger>();
            zt.id = "EV_control_room"; zt.size = bc.size;
            zt.condition = "BRINCAR/CAÇAR, primeira entrada"; zt.effect = "D2 fecha e tranca sozinha";

            var ed = sys.AddComponent<EventDirector>();
            var crate = GameObject.Find("Map/Blocks/crate/crate_ring_NE_movable");
            if (crate) Set(ed, "movableCrate", crate.transform);

            sys.AddComponent<ShiftEnd>();
            var ad = sys.AddComponent<AudioDirector>();
            var z = GameObject.Find("Zatan");
            if (z) Set(ad, "zatanSteps", z.GetComponent<AudioSource>());
        }
    }
}
