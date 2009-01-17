using System;
using System.Collections.Generic;
using System.Text;
using Duplicati.Datamodel;
using Duplicati.Library.Main;

namespace Duplicati.GUI
{
    /// <summary>
    /// This class translates tasks into Duplicati calls, and executes them
    /// </summary>
    public class DuplicatiRunner
    {
        public void ExecuteTask(IDuplicityTask task)
        {
            Dictionary<string, string> options = new Dictionary<string,string>();
            if (string.IsNullOrEmpty(task.Task.Encryptionkey))
                options.Add("no-encryption", "");
            else
                options.Add("passphrase", task.Task.Encryptionkey);


            task.GetOptions(options);
            string results = "";

            switch (task.TaskType)
            {
                case DuplicityTaskType.FullBackup:
                case DuplicityTaskType.IncrementalBackup:
                    results = Interface.Backup(task.SourcePath, task.TargetPath, options);
                    break;

                case DuplicityTaskType.ListBackups:

                    List<string> res = new List<string>();
                    foreach (BackupEntry be in Interface.ParseFileList(task.SourcePath, options))
                    {
                        res.Add(be.Time.ToString());
                        foreach (BackupEntry bei in be.Incrementals)
                            res.Add(bei.Time.ToString());
                    }

                    (task as ListBackupsTask).Backups = res.ToArray();
                    break;
                case DuplicityTaskType.ListFiles:
                    return;

                case DuplicityTaskType.RemoveAllButNFull:
                    results = Interface.RemoveAllButNFull(task.SourcePath, options);
                    return;
                case DuplicityTaskType.RemoveOlderThan:
                    results = Interface.RemoveOlderThan(task.SourcePath, options);
                    return;
                case DuplicityTaskType.Restore:
                    results = Interface.Restore(task.SourcePath, task.TargetPath, options);
                    return;
                default:
                    return;
            }

            task.RaiseTaskCompleted(results);

            if (task.TaskType == DuplicityTaskType.FullBackup || task.TaskType == DuplicityTaskType.IncrementalBackup)
            {
                if (task.Schedule.KeepFull > 0)
                    ExecuteTask(new RemoveAllButNFullTask(task.Schedule, (int)task.Schedule.KeepFull));
                if (!string.IsNullOrEmpty(task.Schedule.KeepTime))
                    ExecuteTask(new RemoveOlderThanTask(task.Schedule, task.Schedule.KeepTime));
            }
        }


        private void PerformBackup(Schedule schedule, bool forceFull, string fullAfter)
        {
            if (forceFull)
                ExecuteTask(new FullBackupTask(schedule));
            else
                ExecuteTask(new IncrementalBackupTask(schedule, fullAfter));
        }

        public void Restore(Schedule schedule, DateTime when, string where)
        {
            ExecuteTask(new RestoreTask(schedule, where, when));
        }

        public string[] ListBackups(Schedule schedule)
        {
            ListBackupsTask task = new ListBackupsTask(schedule);
            ExecuteTask(task);
            return task.Backups;
        }

        public void IncrementalBackup(Schedule schedule)
        {
            PerformBackup(schedule, false, null);
        }

        public void FullBackup(Schedule schedule)
        {
            PerformBackup(schedule, true, null);
        }

    }
}
