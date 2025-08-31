using System.Collections.Generic;
using OpenTK.Mathematics;

namespace WirePC;

// Nivel más bajo: Vertice (polilínea). Si 'Cerrado' es true, une último con primero.
public class Vertice
{
    public List<Vector3> Puntos { get; } = new();
    public bool Cerrado { get; set; }

    public Vertice(IEnumerable<Vector3> puntos, bool cerrado = true)
    {
        Puntos.AddRange(puntos);
        Cerrado = cerrado;
    }

    public IEnumerable<(Vector3 A, Vector3 B)> Segmentar()
    {
        if (Puntos.Count < 2) yield break;
        for (int i = 0; i < Puntos.Count - 1; i++)
            yield return (Puntos[i], Puntos[i + 1]);
        if (Cerrado)
            yield return (Puntos[^1], Puntos[0]);
    }
}

// Cara: lista de contornos (uno principal + agujeros si existiera).
public class Cara
{
    public List<Vertice> Contornos { get; } = new();

    public Cara(IEnumerable<Vertice> contornos)
    {
        Contornos.AddRange(contornos);
    }
}

// Parte: lista de caras + helper Caja (6 caras rectangulares).
public class Parte
{
    public string Nombre { get; }
    public List<Cara> Caras { get; } = new();

    public Parte(string nombre) { Nombre = nombre; }

    public IEnumerable<(Vector3 A, Vector3 B)> TodasLasAristas()
    {
        foreach (var cara in Caras)
            foreach (var cont in cara.Contornos)
                foreach (var seg in cont.Segmentar())
                    yield return seg;
    }

    public static Parte Caja(string nombre, Vector3 centro, Vector3 tam)
    {
        var p = new Parte(nombre);
        var hx = tam.X * 0.5f; var hy = tam.Y * 0.5f; var hz = tam.Z * 0.5f;

        var p000 = centro + new Vector3(-hx, -hy, -hz);
        var p001 = centro + new Vector3(-hx, -hy,  hz);
        var p010 = centro + new Vector3(-hx,  hy, -hz);
        var p011 = centro + new Vector3(-hx,  hy,  hz);
        var p100 = centro + new Vector3( hx, -hy, -hz);
        var p101 = centro + new Vector3( hx, -hy,  hz);
        var p110 = centro + new Vector3( hx,  hy, -hz);
        var p111 = centro + new Vector3( hx,  hy,  hz);

        Cara Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d)
            => new Cara(new[]{ new Vertice(new[]{ a,b,c,d }, true) });

        p.Caras.Add(Quad(p001, p101, p111, p011)); // frontal z+
        p.Caras.Add(Quad(p100, p000, p010, p110)); // trasera z-
        p.Caras.Add(Quad(p000, p001, p011, p010)); // izquierda x-
        p.Caras.Add(Quad(p101, p100, p110, p111)); // derecha x+
        p.Caras.Add(Quad(p000, p100, p101, p001)); // abajo y-
        p.Caras.Add(Quad(p010, p011, p111, p110)); // arriba y+

        return p;
    }
}

// Objeto: lista de partes (monitor, teclado, case).
public class Objeto
{
    public string Nombre { get; }
    public List<Parte> Partes { get; } = new();
    public Objeto(string nombre) { Nombre = nombre; }

    public IEnumerable<(Vector3 A, Vector3 B)> TodasLasAristas()
    {
        foreach (var parte in Partes)
            foreach (var seg in parte.TodasLasAristas())
                yield return seg;
    }
}

// Escena: crea el PC (3 partes) y genera buffer de líneas.
public class EscenaNuevaArquitectura
{
    public List<Objeto> Objetos { get; } = new();

    public EscenaNuevaArquitectura()
    {
        var parteMonitor = ConstruirMonitor(new Vector3(0.0f, 0.5f, 0.0f));
        var parteTeclado = Parte.Caja("Teclado", new Vector3(0.0f, -0.3f, 0.5f), new Vector3(1.2f, 0.05f, 0.4f));
        var parteCase    = Parte.Caja("Case",    new Vector3(1.3f, 0.1f, -0.1f), new Vector3(0.4f, 0.9f, 0.45f));

        var pc = new Objeto("PC de Escritorio");
        pc.Partes.Add(parteMonitor);
        pc.Partes.Add(parteTeclado);
        pc.Partes.Add(parteCase);

        Objetos.Add(pc);
    }

    static Parte ConstruirMonitor(Vector3 centro)
    {
        var pantalla = Parte.Caja("Pantalla", centro + new Vector3(0, 0.4f, 0),  new Vector3(1.6f, 1.0f, 0.05f));
        var soporte  = Parte.Caja("Soporte",  centro + new Vector3(0, -0.05f, -0.05f), new Vector3(0.1f, 0.3f, 0.1f));
        var basePlana= Parte.Caja("Base",     centro + new Vector3(0, -0.25f, 0), new Vector3(0.6f, 0.05f, 0.3f));

        var monitor = new Parte("Monitor");
        monitor.Caras.AddRange(pantalla.Caras);
        monitor.Caras.AddRange(soporte.Caras);
        monitor.Caras.AddRange(basePlana.Caras);
        return monitor;
    }

    public float[] BuildWireframeVertices()
    {
        var list = new List<float>();
        var edges = new HashSet<(Vector3, Vector3)>();

        static (Vector3, Vector3) Key(Vector3 p, Vector3 q)
        {
            if (p.X != q.X) return (p.X < q.X) ? (p,q) : (q,p);
            if (p.Y != q.Y) return (p.Y < q.Y) ? (p,q) : (q,p);
            return (p.Z <= q.Z) ? (p,q) : (q,p);
        }

        void AddEdge(Vector3 a, Vector3 b)
        {
            var k = Key(a,b);
            if (edges.Add(k))
            {
                list.Add(a.X); list.Add(a.Y); list.Add(a.Z);
                list.Add(b.X); list.Add(b.Y); list.Add(b.Z);
            }
        }

        foreach (var obj in Objetos)
            foreach (var (A,B) in obj.TodasLasAristas())
                AddEdge(A,B);

        return list.ToArray();
    }
}
