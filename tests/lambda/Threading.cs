/*
 * Phosphorus Five, copyright 2014 - 2015, Thomas Hansen, phosphorusfive@gmail.com
 * Phosphorus Five is licensed under the terms of the MIT license, see the enclosed LICENSE file for details
 */

using NUnit.Framework;

namespace p5.unittests.threading
{
    /// <summary>
    ///     unit tests for testing the threading features of p5.lambda
    /// </summary>
    [TestFixture]
    public class Threading : TestBase
    {
        public Threading ()
            : base ("p5.types", "p5.hyperlisp", "p5.threading") { }

        /// <summary>
        ///     forks [multiple] threads using [fork], using a [wait] and a [lock] inside of thread lambda,
        ///     making sure all threads are capable of returning values through the [_wait] reference node
        /// </summary>
        [Test]
        public void Threading01 ()
        {
            var node = ExecuteLambda (@"_exe
  lock:job
    add:x:/././*/_wait/#/*/_foos?node
      src:x:/../*(/foo1|/foo2|/foo3|/foo4)?node
wait
  fork:x:/../*/_exe?node
    foo1:bar1
  fork:x:/../*/_exe?node
    foo2:bar2
  fork:x:/../*/_exe?node
    foo3:bar3
  fork:x:/../*/_exe?node
    foo4:bar4
  _foos");
            Assert.AreEqual (4, node [1] [4].Count);
        }
        
        /// <summary>
        ///     Forks a single thread, accessing shared object, making sure [fork] works as it should, and
        ///     passing by reference into threads works as it should.
        /// </summary>
        [Test]
        public void Threading02 ()
        {
            var node = ExecuteLambda (@"_foo
set:x:/+/0?value
  src:x:/./-?node
fork
  _foo
  lock:foo
    add:x:/./-/#?node
      src
        bar:thread
sleep:100
lock:foo
  add:x:/../""*""/_foo?node
    src
      bar:main");
            Assert.AreEqual (2, node [0].Count);
        }
    }
}