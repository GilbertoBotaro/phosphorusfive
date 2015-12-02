/*
 * Phosphorus Five, copyright 2014 - 2015, Thomas Hansen, phosphorusfive@gmail.com
 * Phosphorus Five is licensed under the terms of the MIT license, see the enclosed LICENSE file for details
 */

using System.IO;
using p5.core;
using p5.exp;
using p5.exp.exceptions;

namespace p5.file.file
{
    /// <summary>
    ///     Class to help save files
    /// </summary>
    public static class Save
    {
        /// <summary>
        ///     Saves a file to disc
        /// </summary>
        /// <param name="context">Application context</param>
        /// <param name="e">Parameters passed into Active Event</param>
        [ActiveEvent (Name = "save-file")]
        [ActiveEvent (Name = "save-text-file")]
        private static void save_file (ApplicationContext context, ActiveEventArgs e)
        {
            // Making sure we clean up and remove all arguments passed in after execution
            using (new Utilities.ArgsRemover (e.Args, true)) {

                // Getting root folder
                var rootFolder = Common.GetRootFolder (context);

                // Getting filename
                string fileName = XUtil.Single<string> (context, e.Args);

                // Verifying user is allowed to save to file
                context.Raise ("_authorize-save-file", new Node ("_authorize-save-file", fileName).Add ("args", e.Args));

                // Getting source
                var source = Utilities.Convert<string> (context, XUtil.SourceSingle (context, e.Args));

                // Saving file
                using (TextWriter writer = File.CreateText (rootFolder + fileName)) {
                    writer.Write (source);
                }
            }
        }

        /// <summary>
        ///     Saves a binary file to disc
        /// <param name="context">Application context</param>
        /// <param name="e">Parameters passed into Active Event</param>
        [ActiveEvent (Name = "save-binary-file")]
        private static void save_binary_file (ApplicationContext context, ActiveEventArgs e)
        {
            // Making sure we clean up and remove all arguments passed in after execution
            using (new Utilities.ArgsRemover (e.Args, true)) {

                // Getting root folder
                var rootFolder = Common.GetRootFolder (context);

                // Getting filename
                string fileName = XUtil.Single<string> (context, e.Args);

                // Verifying user is allowed to save to file
                context.Raise ("_authorize-save-file", new Node ("_authorize-save-file", fileName).Add ("args", e.Args));

                // Getting source
                var source = Utilities.Convert<byte[]> (context, XUtil.SourceSingle (context, e.Args));

                // Saving file
                using (FileStream stream = File.Create (rootFolder + fileName)) {
                    stream.Write (source, 0, source.Length);
                }
            }
        }
    }
}
