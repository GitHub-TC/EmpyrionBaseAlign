using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Eleon.Modding;

namespace EmpyrionBaseAlign.Test
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void TestNullPosRot()
        {
            var M = new IdPositionRotation() { id = 1, pos = new PVector3(), rot = new PVector3() };
            var A = new IdPositionRotation() { id = 1, pos = new PVector3(), rot = new PVector3() };
            var R = EmpyrionBaseAlign.ExecAlign(M, A, Vector3.Zero, Vector3.Zero);
            Assert.AreEqual(0, R.pos.x);
            Assert.AreEqual(0, R.pos.y);
            Assert.AreEqual(0, R.pos.z);

            Assert.AreEqual(0, R.rot.x);
            Assert.AreEqual(0, R.rot.y);
            Assert.AreEqual(0, R.rot.z);
        }

        [TestMethod]
        public void TestAlignedPosRot()
        {
            var M = new IdPositionRotation() { id = 1, pos = new PVector3(),         rot = new PVector3() };
            var A = new IdPositionRotation() { id = 1, pos = new PVector3(20,20,20), rot = new PVector3() };
            var R = EmpyrionBaseAlign.ExecAlign(M, A, Vector3.Zero, Vector3.Zero);
            Assert.AreEqual(20, R.pos.x);
            Assert.AreEqual(20, R.pos.y);
            Assert.AreEqual(20, R.pos.z);

            Assert.AreEqual(0, R.rot.x);
            Assert.AreEqual(0, R.rot.y);
            Assert.AreEqual(0, R.rot.z);
        }

        [TestMethod]
        public void TestPosRotX()
        {
            var M = new IdPositionRotation() { id = 1, pos = new PVector3(),           rot = new PVector3(45,0,0) };
            var A = new IdPositionRotation() { id = 1, pos = new PVector3(20, 20, 20), rot = new PVector3() };
            var R = EmpyrionBaseAlign.ExecAlign(M, A, Vector3.Zero, Vector3.Zero);
            Assert.AreEqual(19.799, Math.Round(R.pos.x, 3));
            Assert.AreEqual(19.799, Math.Round(R.pos.y, 3));
            Assert.AreEqual(20    , Math.Round(R.pos.z, 3));

            Assert.AreEqual(45, R.rot.x);
            Assert.AreEqual( 0, R.rot.y);
            Assert.AreEqual( 0, R.rot.z);
        }

        [TestMethod]
        public void TestPosRotDoubleX()
        {
            var M = new IdPositionRotation() { id = 1, pos = new PVector3(), rot = new PVector3(45, 0, 0) };
            var A = new IdPositionRotation() { id = 1, pos = new PVector3(20, 20, 20), rot = new PVector3() };

            var R = EmpyrionBaseAlign.ExecAlign(M, A, Vector3.Zero, Vector3.Zero);
                R = EmpyrionBaseAlign.ExecAlign(M, R, Vector3.Zero, Vector3.Zero);

            Assert.AreEqual(19.799, Math.Round(R.pos.x, 3));
            Assert.AreEqual(19.799, Math.Round(R.pos.y, 3));
            Assert.AreEqual(20,     Math.Round(R.pos.z, 3));

            Assert.AreEqual(45, R.rot.x);
            Assert.AreEqual(0, R.rot.y);
            Assert.AreEqual(0, R.rot.z);
        }

        [TestMethod]
        public void TestPosRotY()
        {
            var M = new IdPositionRotation() { id = 1, pos = new PVector3(), rot = new PVector3(0, 45, 0) };
            var A = new IdPositionRotation() { id = 1, pos = new PVector3(20, 20, 20), rot = new PVector3() };
            var R = EmpyrionBaseAlign.ExecAlign(M, A, Vector3.Zero, Vector3.Zero);
            Assert.AreEqual(19.799, Math.Round(R.pos.x, 3));
            Assert.AreEqual(20,     Math.Round(R.pos.y, 3));
            Assert.AreEqual(19.799, Math.Round(R.pos.z, 3));

            Assert.AreEqual(0,  R.rot.x);
            Assert.AreEqual(45, R.rot.y);
            Assert.AreEqual(0,  R.rot.z);
        }

        [TestMethod]
        public void TestPosRotDoubleY()
        {
            var M = new IdPositionRotation() { id = 1, pos = new PVector3(), rot = new PVector3(0, 45, 0) };
            var A = new IdPositionRotation() { id = 1, pos = new PVector3(20, 20, 20), rot = new PVector3() };

            var R = EmpyrionBaseAlign.ExecAlign(M, A, Vector3.Zero, Vector3.Zero);
                R = EmpyrionBaseAlign.ExecAlign(M, R, Vector3.Zero, Vector3.Zero);

            Assert.AreEqual(19.799, Math.Round(R.pos.x, 3));
            Assert.AreEqual(20,     Math.Round(R.pos.y, 3));
            Assert.AreEqual(19.799, Math.Round(R.pos.z, 3));

            Assert.AreEqual(0,  R.rot.x);
            Assert.AreEqual(45, R.rot.y);
            Assert.AreEqual(0,  R.rot.z);
        }

        [TestMethod]
        public void TestPosRotZ()
        {
            var M = new IdPositionRotation() { id = 1, pos = new PVector3(), rot = new PVector3(0, 0, 45) };
            var A = new IdPositionRotation() { id = 1, pos = new PVector3(20, 20, 20), rot = new PVector3() };
            var R = EmpyrionBaseAlign.ExecAlign(M, A, Vector3.Zero, Vector3.Zero);
            Assert.AreEqual( 20,     Math.Round(R.pos.x, 3));
            Assert.AreEqual( 19.799, Math.Round(R.pos.y, 3));
            Assert.AreEqual( 19.799, Math.Round(R.pos.z, 3));

            Assert.AreEqual(0,  R.rot.x);
            Assert.AreEqual(0,  R.rot.y);
            Assert.AreEqual(45, R.rot.z);
        }

        [TestMethod]
        public void TestPosRotDoubleZ()
        {
            var M = new IdPositionRotation() { id = 1, pos = new PVector3(), rot = new PVector3(0, 0, 45) };
            var A = new IdPositionRotation() { id = 1, pos = new PVector3(20, 20, 20), rot = new PVector3() };

            var R = EmpyrionBaseAlign.ExecAlign(M, A, Vector3.Zero, Vector3.Zero);
                R = EmpyrionBaseAlign.ExecAlign(M, R, Vector3.Zero, Vector3.Zero);

            Assert.AreEqual(20,     Math.Round(R.pos.x, 3));
            Assert.AreEqual(19.799, Math.Round(R.pos.y, 3));
            Assert.AreEqual(19.799, Math.Round(R.pos.z, 3));

            Assert.AreEqual(0,  R.rot.x);
            Assert.AreEqual(0,  R.rot.y);
            Assert.AreEqual(45, R.rot.z);
        }

        [TestMethod]
        public void TestPosRotXYZ()
        {
            var M = new IdPositionRotation() { id = 1, pos = new PVector3(), rot = new PVector3(45, 45, 45) };
            var A = new IdPositionRotation() { id = 1, pos = new PVector3(20, 20, 20), rot = new PVector3() };
            var R = EmpyrionBaseAlign.ExecAlign(M, A, Vector3.Zero, Vector3.Zero);
            Assert.AreEqual(19.971, Math.Round(R.pos.x, 3));
            Assert.AreEqual(19.757, Math.Round(R.pos.y, 3));
            Assert.AreEqual(19.971, Math.Round(R.pos.z, 3));

            Assert.AreEqual(45, R.rot.x);
            Assert.AreEqual(45, R.rot.y);
            Assert.AreEqual(45, R.rot.z);
        }

        [TestMethod]
        public void TestPosRotDoubleXYZ()
        {
            var M = new IdPositionRotation() { id = 1, pos = new PVector3(), rot = new PVector3(45, 45, 45) };
            var A = new IdPositionRotation() { id = 1, pos = new PVector3(20, 20, 20), rot = new PVector3() };

            var R = EmpyrionBaseAlign.ExecAlign(M, A, Vector3.Zero, Vector3.Zero);
                R = EmpyrionBaseAlign.ExecAlign(M, R, Vector3.Zero, Vector3.Zero);

            Assert.AreEqual(19.971, Math.Round(R.pos.x, 3));
            Assert.AreEqual(19.757, Math.Round(R.pos.y, 3));
            Assert.AreEqual(19.971, Math.Round(R.pos.z, 3));

            Assert.AreEqual(45, R.rot.x);
            Assert.AreEqual(45, R.rot.y);
            Assert.AreEqual(45, R.rot.z);
        }


    }
}
