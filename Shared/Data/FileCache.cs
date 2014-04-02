using System;
using System.Threading.Tasks;
using System.Text;
using System.IO;
using System.Net;
using System.Collections.Generic;
using System.Net.Http;
using PCLStorage;
using System.Diagnostics;

namespace XamarinStore
{
	public static class FileCache
	{
		static FileCache()
		{
			init ();
		}
		public static Func<string,bool> FileExists;
		static bool initialized;
		static IFolder tempfolder;
		static IFolder imageFolder;
		static async Task<bool> init()
		{
			try{
			if (initialized)
				return true;
			IFolder rootFolder = FileSystem.Current.LocalStorage;
			tempfolder = await rootFolder.CreateFolderAsync ("Cache",
				CreationCollisionOption.OpenIfExists);
			imageFolder = await rootFolder.CreateFolderAsync ("Images",
				CreationCollisionOption.OpenIfExists);
			}
			catch(Exception ex) {
				Debug.WriteLine (ex);
			}
			return initialized = true;
		}

		public static async Task<string> Download(string url)
		{
			while (!initialized)
				await Task.Delay (500);
			var fileName = GetFileName(url);

			var downloadTask = Download (url, fileName);
			if (downloadTask.IsCompleted)
				return downloadTask.Result;
			return await downloadTask;
		}

		static object locker = new object ();
		public static async Task<string> Download(string url, string fileName)
		{
			try{
				var path = Path.Combine (tempfolder.Path, fileName);
				var destination = Path.Combine(imageFolder.Path,fileName);
				if(FileExists != null && FileExists(destination))
					return destination;
				var exists = await imageFolder.CheckExistsAsync(fileName);
				if (exists == ExistenceCheckResult.FileExists)
				{
					return destination;
				}

				var succes = await GetDownload(url,path,destination);
				return succes  ? destination : "";
			}
			catch(Exception ex) {
				Debug.WriteLine (ex);
				return  "";
			}
		}

		static Dictionary<string,Task<bool>> downloadTasks = new Dictionary<string, Task<bool>> ();
		static Task<bool> GetDownload(string url, string fileName,string destination)
		{
			lock (locker) {
				Task<bool> task;
				if (downloadTasks.TryGetValue (fileName, out task))
					return task;

				downloadTasks.Add (fileName, task = download (url, fileName,destination));
				return task;

			}
		}
		static async Task<bool> download(string url, string fileName,string destination)
		{ 
			IFile file = null;
			try{
				var client = new HttpClient ();
				var data = await client.GetByteArrayAsync (url);
				file = await tempfolder.CreateFileAsync (fileName,
					CreationCollisionOption.ReplaceExisting);
				using(var fileStream = await file.OpenAsync (FileAccess.ReadAndWrite)){
					fileStream.Write (data, 0, data.Length);
				}
				if(ProcessImage != null)
					await ProcessImage(fileName,destination);
				else
					await file.MoveAsync(destination);
				return true;
			}
			catch(Exception ex) {
				Debug.WriteLine (ex);
			}
			if (file != null)
				await file.DeleteAsync ();
			return false;
		}
		static void removeTask(string fileName)
		{
			lock (locker) {
				downloadTasks.Remove (fileName);
			}
		}

		public static Func<string,string,Task> ProcessImage;
		static string GetFileName(String hrefLink)
		{
			return MD5Core.GetHashString(hrefLink);
		}


	}
}

