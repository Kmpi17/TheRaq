using System.Collections.Generic;
using UnityEngine;

namespace GTAChiraq
{
    /// <summary>
    /// Panel de valla (madera, hierro o malla) de <c>PrototypeSceneBuilder.FenceSegment</c>: una caja fina con la textura
    /// repetida, su <c>BoxCollider</c>, los postes y la barandilla de arriba. Al romperla, el coche abre un hueco de unos
    /// 3 m donde da (<see cref="BreakMath.SplitFence"/>): el panel original se esconde y se rehace en trozos; los de los
    /// lados siguen en pie (y se pueden volver a romper) y los del hueco salen volando con sus postes.
    /// El panel original puede estar en un lote estático (static batching): no se mueve, solo se esconde.
    /// </summary>
    [RequireComponent(typeof(BoxCollider), typeof(MeshRenderer))]
    public sealed class BreakableFence : Breakable
    {
        public enum Kind { Wood, Iron, Chain }

        const float Gap = 2.8f, MinKeep = 0.6f, MaxPiece = 1.5f;

        [SerializeField] Vector2 tile = Vector2.one;
        [SerializeField] bool sidesOnly = true;
        [SerializeField] Transform[] posts = new Transform[0];
        [SerializeField] Transform rail;
        [SerializeField] Kind kind = Kind.Iron;

        static AudioClip woodClip, ironClip, chainClip;
        static readonly List<BreakMath.Span> kept = new(), flying = new();

        public Kind FenceKind => kind;

        public void Configure(Vector2 textureTile, bool noCaps, Transform[] fencePosts, Transform topRail, Kind fenceKind)
        {
            tile = textureTile;
            sidesOnly = noCaps;
            posts = fencePosts ?? new Transform[0];
            rail = topRail;
            kind = fenceKind;
        }

        protected override void Break(Vector3 velocity, Vector3 point, Collider[] ignore)
        {
            var box = GetComponent<BoxCollider>();
            var look = GetComponent<MeshRenderer>();
            var size = Vector3.Scale(box.size, transform.lossyScale);
            float hit = transform.InverseTransformPoint(point).x * transform.lossyScale.x;
            BreakMath.SplitFence(size.x, hit, Gap, MinKeep, MaxPiece, kept, flying);

            look.enabled = false;
            box.enabled = false;
            var right = Vector3.Cross(Vector3.up, velocity).normalized;

            // La barandilla de arriba: sus extremos a lo largo de la valla.
            Vector3 railA = default, railB = default;
            float railThick = 0f;
            Material railMat = null;
            if (rail && rail.TryGetComponent<MeshRenderer>(out var railLook))
            {
                railA = rail.position - rail.up * rail.lossyScale.y;
                railB = rail.position + rail.up * rail.lossyScale.y;
                railThick = rail.lossyScale.x;
                railMat = railLook.sharedMaterial;
                railLook.enabled = false;
            }

            foreach (var s in kept)
            {
                var piece = Panel(s, size, size.z, look);
                piece.AddComponent<BoxCollider>().size = new Vector3(s.Length, size.y, size.z);
                var newRail = railMat ? RailPiece(s, railA, railB, railThick, railMat, piece.transform.parent) : null;
                var fence = piece.AddComponent<BreakableFence>();
                fence.Configure(tile, sidesOnly, PostsIn(s), newRail, kind);
                fence.SetStrength(minSpeed, speedKept);
            }

            foreach (var s in flying)
            {
                var piece = Panel(s, size, Mathf.Max(size.z, 0.08f), look);
                piece.AddComponent<BoxCollider>().size = new Vector3(s.Length, size.y, Mathf.Max(size.z, 0.08f));
                if (railMat) RailPiece(s, railA, railB, railThick, railMat, piece.transform);
                float side = Mathf.Sign(Vector3.Dot(piece.transform.position - point, right));
                float mass = (kind == Kind.Wood ? 14f : 9f) * s.Length * size.y;
                Debris.Launch(piece, mass, BreakMath.LaunchVelocity(velocity, right, side) + Random.insideUnitSphere,
                    Random.insideUnitSphere * 5f, ignore);
            }

            // Los postes que caen en el hueco salen volando también.
            foreach (var post in posts)
            {
                if (!post || !post.TryGetComponent<MeshRenderer>(out var postLook) || !postLook.enabled) continue;
                float x = transform.InverseTransformPoint(post.position).x * transform.lossyScale.x;
                bool standing = false;
                foreach (var s in kept) if (s.Contains(x, 0.05f)) standing = true;
                if (standing) continue;
                postLook.enabled = false;
                var copy = new GameObject(post.name);
                copy.transform.SetPositionAndRotation(post.position, post.rotation);
                copy.transform.localScale = post.lossyScale;
                copy.AddComponent<MeshFilter>().sharedMesh = ProceduralMeshes.Cylinder;
                copy.AddComponent<MeshRenderer>().sharedMaterial = postLook.sharedMaterial;
                copy.AddComponent<CapsuleCollider>();
                float side = Mathf.Sign(Vector3.Dot(post.position - point, right));
                Debris.Launch(copy, 10f, BreakMath.LaunchVelocity(velocity, right, side, 0.6f), Random.insideUnitSphere * 6f, ignore);
            }

            if (!woodClip)
            {
                woodClip = ProceduralSfx.WoodBreak();
                ironClip = ProceduralSfx.MetalClang();
                chainClip = ProceduralSfx.FenceRattle();
            }
            Play(kind == Kind.Wood ? woodClip : kind == Kind.Chain ? chainClip : ironClip, point, 0.9f);
        }

        /// <summary>Un trozo del panel entre <c>s.From</c> y <c>s.To</c>, con la misma textura y material.</summary>
        GameObject Panel(BreakMath.Span s, Vector3 size, float thickness, MeshRenderer look)
        {
            var go = new GameObject(name);
            go.layer = gameObject.layer;
            go.transform.SetParent(transform.parent, false);
            go.transform.SetPositionAndRotation(transform.position + transform.right * s.Mid, transform.rotation);
            go.AddComponent<MeshFilter>().sharedMesh = ProceduralMeshes.Box(name, new Vector3(s.Length, size.y, thickness), tile, sidesOnly);
            var r = go.AddComponent<MeshRenderer>();
            r.sharedMaterial = look.sharedMaterial;
            r.shadowCastingMode = look.shadowCastingMode;
            return go;
        }

        /// <summary>El trozo de barandilla que va encima de <paramref name="s"/> (o null si no llega).</summary>
        Transform RailPiece(BreakMath.Span s, Vector3 a, Vector3 b, float thick, Material mat, Transform parent)
        {
            float xa = transform.InverseTransformPoint(a).x * transform.lossyScale.x;
            float xb = transform.InverseTransformPoint(b).x * transform.lossyScale.x;
            if (Mathf.Abs(xb - xa) < 0.01f) return null;
            float t0 = Mathf.Clamp01(Mathf.InverseLerp(xa, xb, s.From)), t1 = Mathf.Clamp01(Mathf.InverseLerp(xa, xb, s.To));
            Vector3 p = Vector3.Lerp(a, b, t0), q = Vector3.Lerp(a, b, t1);
            float length = Vector3.Distance(p, q);
            if (length < 0.1f) return null;
            var go = new GameObject("Barandilla");
            go.layer = parent ? parent.gameObject.layer : gameObject.layer;
            go.transform.SetParent(parent, true);
            go.transform.SetPositionAndRotation((p + q) / 2f, Quaternion.FromToRotation(Vector3.up, q - p));
            go.transform.localScale = Vector3.one;
            var lossy = parent ? parent.lossyScale : Vector3.one;
            go.transform.localScale = new Vector3(thick / lossy.x, length / 2f / lossy.y, thick / lossy.z);
            go.AddComponent<MeshFilter>().sharedMesh = ProceduralMeshes.Cylinder;
            go.AddComponent<MeshRenderer>().sharedMaterial = mat;
            return go.transform;
        }

        Transform[] PostsIn(BreakMath.Span s)
        {
            var list = new List<Transform>();
            foreach (var p in posts)
                if (p && s.Contains(transform.InverseTransformPoint(p.position).x * transform.lossyScale.x, 0.05f)) list.Add(p);
            return list.ToArray();
        }
    }
}
