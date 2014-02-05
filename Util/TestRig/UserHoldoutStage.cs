using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RecognitionManager;
using System.IO;

namespace TestRig
{
    class UserHoldoutStage : ProcessStage
    {
        List<string> _fileNames;
        Dictionary<string, List<string>> _userSketches;

        public UserHoldoutStage()
        {
            name = "User Holdout";
            shortname = "uhcv";
            outputFiletype = ".csv";
            _fileNames = new List<string>();
            _userSketches = new Dictionary<string, List<string>>();
        }

        public override void run(Sketch.Sketch sketch, string filename)
        {
            _fileNames.Add(filename);
            string[] parts = filename.Split(new char[] { '\\' });
            string shortfilename = parts.Last();
            string user = shortfilename.Split(new char[] { '_' }).First();

            if (!_userSketches.ContainsKey(user))
                _userSketches.Add(user, new List<string>());

            _userSketches[user].Add(filename);
        }

        public override void finalize(TextWriter tw, string path)
        {
            foreach (string testUser in _userSketches.Keys)
            {
                Utilities.WekaWrap.wekaSetUp(Utilities.WekaWrap.Classifier.AdaBoost_J48, _fileNames, testUser);

                TestRig rig = new TestRig();
                rig.OutputPath = path;
                rig.Pause = false;

                ProcessStage classify = new ClassifyStage();
                GroupStage puregroup = new GroupStage();
                puregroup.IsPure = true;

                GroupStage impuregroup = new GroupStage();
                impuregroup.IsPure = false;

                rig.addStage(classify);
                rig.addStage(puregroup);
                rig.addStage(impuregroup);

                foreach (string testFile in _userSketches[testUser])
                    rig.AddFile(testFile);

                rig.run();
            }
        }
    }
}
