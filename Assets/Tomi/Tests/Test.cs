using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class NewTestScript
{
    public struct VectorData
    {
        public float Magnitude;
        private List<Vector2> _vector2s;
        public VectorData(List<Vector2> vector2s, float mul)
        {
            vector2s[1] *= mul;
            _vector2s = vector2s;
            Magnitude = (_vector2s[0] - _vector2s[1]).magnitude;
        }
    }
    public class VectorDataC
    {
        public float Magnitude;
        private List<Vector2> _vector2s;
        public VectorDataC(List<Vector2> vector2s, float mul)
        {
            vector2s[1] *= mul;
            _vector2s = vector2s;
            Magnitude = (_vector2s[0] - _vector2s[1]).magnitude;
        }
    }
    // A Test behaves as an ordinary method
    [Test]
    public void NewTestScriptSimplePasses()
    {
        List<Vector2> v1 = new List<Vector2> {Vector2.one, Vector2.one};
        var mul = 2f;
        var vd = new VectorData(v1 , mul);
        var vc = new VectorDataC(v1, mul);
        Assert.That(vd.Magnitude > 1f);
        Assert.That(vc.Magnitude > 1f);
        v1[1] *= 2;
        Assert.That(vd.Magnitude > 2f);
        Assert.That(vc.Magnitude > 2f);
        Assert.IsTrue(true);
    }

    // A UnityTest behaves like a coroutine in Play Mode. In Edit Mode you can use
    // `yield return null;` to skip a frame.
    [UnityTest]
    public IEnumerator NewTestScriptWithEnumeratorPasses()
    {
        // Use the Assert class to test conditions.
        // Use yield to skip a frame.
        yield return null;
    }
}
