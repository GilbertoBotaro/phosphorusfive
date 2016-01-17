/*
 * Phosphorus Five, copyright 2014 - 2015, Thomas Hansen, phosphorusfive@gmail.com
 * Phosphorus Five is licensed under the terms of the MIT license, see the enclosed LICENSE file for details
 */

using System;
using System.Collections.Generic;
using p5.core;

namespace p5.exp.iterators
{
    /// <summary>
    ///     Special Iterator for grouping iterators
    /// </summary>
    [Serializable]
    public class IteratorGroup : Iterator
    {
        /// <summary>
        ///     Root iterators for nested IteratorGroup iterators
        /// </summary>
        [Serializable]
        private class IteratorLeftParent : Iterator
        {
            private readonly Iterator _leftParent;

            /// <summary>
            ///     Initializes a new instance of the <see cref="phosphorus.expressions.iterators.IteratorLeftParent" /> class
            /// </summary>
            /// <param name="leftParent">The last iterator of the parent group iterator</param>
            public IteratorLeftParent (Iterator leftParent)
            {
                _leftParent = leftParent;
            }

            public override IEnumerable<Node> Evaluate (ApplicationContext context)
            {
                return _leftParent.Evaluate (context);
            }
        }

        private readonly Iterator _groupRoot;
        private readonly List<Logical> _logicals = new List<Logical> ();

        /// <summary>
        ///     Initializes a new instance of the <see cref="phosphorus.expressions.iterators.IteratorGroup" /> class
        /// </summary>
        internal IteratorGroup ()
        {
            _groupRoot = new IteratorIdentity ();
            AddLogical (new Logical (Logical.LogicalType.Or));
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="phosphorus.expressions.iterators.IteratorGroup" /> class
        /// </summary>
        /// <param name="parent">Parent iterator group</param>
        public IteratorGroup (IteratorGroup parent)
        {
            _groupRoot = new IteratorLeftParent (parent.LastIterator);
            AddLogical (new Logical (Logical.LogicalType.Or));
            ParentGroup = parent;
            ParentGroup.AddIterator (this);
        }

        /// <summary>
        ///     Returns the parent group
        /// </summary>
        /// <value>the parent group</value>
        public IteratorGroup ParentGroup
        {
            get;
            private set;
        }
        
        internal Node GroupRootNode {
            get { return ((IteratorIdentity)_groupRoot).RootNode; }
            set { ((IteratorIdentity)_groupRoot).RootNode = value; }
        }

        /// <summary>
        ///     Gets the last iterator in the group
        /// </summary>
        /// <value>the last iterator</value>
        internal Iterator LastIterator
        {
            get { return _logicals [_logicals.Count - 1].Iterator; }
        }

        /// <summary>
        ///     Adds a Logical to the list of logicals in the group
        /// </summary>
        /// <param name="logical">logical</param>
        public void AddLogical (Logical logical)
        {
            _logicals.Add (logical);

            // Making sure "group root iterator" is root iterator for all Logicals
            _logicals [_logicals.Count - 1].AddIterator (_groupRoot);
        }

        /// <summary>
        ///     Appends a new iterator to the last <see cref="phosphorus.expressions.Logical" /> in the group
        /// </summary>
        /// <param name="iterator">Iterator to append</param>
        public void AddIterator (Iterator iterator)
        {
            _logicals [_logicals.Count - 1].AddIterator (iterator);
        }

        public override IEnumerable<Node> Evaluate (ApplicationContext context)
        {
            var nodes = new List<Node> ();
            foreach (var idxLogical in _logicals) {
                nodes = idxLogical.EvaluateNodes (nodes, context);
            }
            return nodes;
        }
    }
}
