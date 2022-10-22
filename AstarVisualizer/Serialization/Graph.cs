using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AstarVisualizer.Serialization;

public sealed class Graph
{
    public record V(float X, float Y);
    public record E(int A, int B);

    public List<V> Vertices { get; set; } = new();


    public Graph FromVertices(IEnumerable<Vertex> vertices)
    {
        Vertex[] array = vertices.ToArray();

        Graph graph = new Graph();



        return graph;
    }
}
