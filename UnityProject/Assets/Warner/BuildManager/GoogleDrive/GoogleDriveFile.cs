using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using GDrive;
using UnityEngine;
using JsonFx.Json;

namespace GDrive
{
    partial class GoogleDrive
    {
        public class File
        {
            public string ID { get; private set; }
            public string Title { get; set; }
            public string MimeType { get; set; }
            public string Description { get; set; }
            public DateTime CreatedDate { get; private set; }
            public DateTime ModifiedDate { get; private set; }
            public string ThumbnailLink { get; private set; }
            public string DownloadUrl { get; private set; }
            public string MD5Checksum { get; private set; }
            public long FileSize { get; private set; }
            public List<string> Parents { get; set; }
            public bool IsFolder
            {
                get { return MimeType == "application/vnd.google-apps.folder"; }
            }


            public File(Dictionary<string, object> metadata)
            {
                ID = TryGet<string>(metadata, "id");

                Title = TryGet<string>(metadata, "title");
                MimeType = TryGet<string>(metadata, "mimeType");
                Description = TryGet<string>(metadata, "description");

                DateTime createdDate;
                DateTime.TryParse(TryGet<string>(metadata, "createdDate"), out createdDate);
                CreatedDate = createdDate;

                DateTime modifiedDate;
                DateTime.TryParse(TryGet<string>(metadata, "modifiedDate"), out modifiedDate);
                ModifiedDate = modifiedDate;

                ThumbnailLink = TryGet<string>(metadata, "thumbnailLink");
                DownloadUrl = TryGet<string>(metadata, "downloadUrl");

                MD5Checksum = TryGet<string>(metadata, "md5Checksum");

                string fileSize = TryGet<string>(metadata, "fileSize");
                if (fileSize != null)
                    FileSize = long.Parse(fileSize);

                // Get parent folders.
                Parents = new List<string>();

                if (metadata.ContainsKey("parents") &&
                metadata["parents"] is Dictionary<string, object>[])
                {
                    var parents = metadata["parents"] as Dictionary<string, object>[];
                    foreach (var parent in parents)
                    {
                        if (parent.ContainsKey("id"))
                            Parents.Add(parent["id"] as string);
                    }
                }
            }

            public Dictionary<string, object> ToJSON()
            {
                var json = new Dictionary<string, object>();
			
                json["title"] = Title;
                json["mimeType"] = MimeType;
                json["description"] = Description;

                var parents = new Dictionary<string, object>[Parents.Count];
                for (int i = 0; i < Parents.Count; i++)
                {
                    parents[i] = new Dictionary<string, object>();
                    parents[i]["id"] = Parents[i];
                }
                json["parents"] = parents;

                return json;
            }

            public override string ToString()
            {
                return string.Format("{{ title: {0}, mimeType: {1}, fileSize: {2}, id: {3}, parents: [{4}] }}",
                    Title, MimeType, FileSize, ID, string.Join(", ", Parents.ToArray()));
            }
        }

        // https://developers.google.com/drive/appdata
        public File AppData { get; private set; }

        public IEnumerator GetFile(string id)
        {
            #region Check the access token is expired
            var check = CheckExpiration();
            while (check.MoveNext())
                yield return null;

            if (check.Current is Exception)
            {
                yield return check.Current;
                yield break;
            }
            #endregion

            var request = new UnityWebRequest(
                    new Uri("https://www.googleapis.com/drive/v2/files/" + id));
            request.headers["Authorization"] = "Bearer " + AccessToken;

            var response = new UnityWebResponse(request);
            while (!response.isDone)
                yield return null;

            JsonReader reader = new JsonReader(response.text);
            Dictionary<string, object> json = reader.Deserialize<Dictionary<string, object>>();

            if (json == null)
            {
                yield return new Exception(-1, "GetFile response parsing failed.");
                yield break;
            }
            else if (json.ContainsKey("error"))
            {
                yield return GetError(json);
                yield break;
            }

            yield return new AsyncSuccess(new File(json));
        }

        public IEnumerator ListAllFiles()
        {
            var listFiles = ListFilesByQueary("");
            while (listFiles.MoveNext())
                yield return listFiles.Current;
        }


        public IEnumerator ListFiles(File parentFolder)
        {
            var listFiles = ListFilesByQueary(string.Format("'{0}' in parents", parentFolder.ID));
            while (listFiles.MoveNext())
                yield return listFiles.Current;
        }

        //https://developers.google.com/drive/search-parameters
       
        public IEnumerator ListFilesByQueary(string query)
        {
            #region Check the access token is expired
            var check = CheckExpiration();
            while (check.MoveNext())
                yield return null;

            if (check.Current is Exception)
            {
                yield return check.Current;
                yield break;
            }
            #endregion

            var request = new UnityWebRequest(
                    new Uri("https://www.googleapis.com/drive/v2/files?q=" + query));
            request.headers["Authorization"] = "Bearer " + AccessToken;

            var response = new UnityWebResponse(request);
            while (!response.isDone)
                yield return null;

            JsonReader reader = new JsonReader(response.text);
Dictionary<string, object> json = reader.Deserialize<Dictionary<string, object>>();

            if (json == null)
            {
                yield return new Exception(-1, "ListFiles response parsing failed.");
                yield break;
            }
            else if (json.ContainsKey("error"))
            {
                yield return GetError(json);
                yield break;
            }

            // parsing
            var results = new List<File>();

            if (json.ContainsKey("items") &&
            json["items"] is Dictionary<string, object>[])
            {
                var items = json["items"] as Dictionary<string, object>[];
                foreach (var item in items)
                {
                    results.Add(new File(item));
                }
            }

            yield return new AsyncSuccess(results);
        }

        public IEnumerator InsertFolder(string title)
        {
            var insertFolder = InsertFolder(title, null);
            while (insertFolder.MoveNext())
                yield return insertFolder.Current;
        }

        public IEnumerator InsertFolder(string title, File parentFolder)
        {
            #region Check the access token is expired
            var check = CheckExpiration();
            while (check.MoveNext())
                yield return null;

            if (check.Current is Exception)
            {
                yield return check.Current;
                yield break;
            }
            #endregion

            var request = new UnityWebRequest("https://www.googleapis.com/drive/v2/files");
            request.method = "POST";
            request.headers["Authorization"] = "Bearer " + AccessToken;
            request.headers["Content-Type"] = "application/json";
		
            Dictionary<string, object> data = new Dictionary<string, object>();
            data["title"] = title;
            data["mimeType"] = "application/vnd.google-apps.folder";
            if (parentFolder != null)
            {
                data["parents"] = new List<Dictionary<string, string>>
                {
                    new Dictionary<string, string>
                    {
                        { "id", parentFolder.ID }
                    },
                };
            }

            request.body = Encoding.UTF8.GetBytes(JsonWriter.Serialize(data));

            var response = new UnityWebResponse(request);
            while (!response.isDone)
                yield return null;

            JsonReader reader = new JsonReader(response.text);
Dictionary<string, object> json = reader.Deserialize<Dictionary<string, object>>();

            if (json == null)
            {
                yield return new Exception(-1, "InsertFolder response parsing failed.");
                yield break;
            }
            else if (json.ContainsKey("error"))
            {
                yield return GetError(json);
                yield break;
            }

            yield return new AsyncSuccess(new File(json));
        }

        public IEnumerator DeleteFile(File file)
        {
            #region Check the access token is expired
            var check = CheckExpiration();
            while (check.MoveNext())
                yield return null;

            if (check.Current is Exception)
            {
                yield return check.Current;
                yield break;
            }
            #endregion

            var request = new UnityWebRequest("https://www.googleapis.com/drive/v2/files/" + file.ID);
            request.method = "DELETE";
            request.headers["Authorization"] = "Bearer " + AccessToken;

            var response = new UnityWebResponse(request);
            while (!response.isDone)
                yield return null;

            // If successful, empty response.
            JsonReader reader = new JsonReader(response.text);
Dictionary<string, object> json = reader.Deserialize<Dictionary<string, object>>();
		
            if (json != null && json.ContainsKey("error"))
            {
                yield return GetError(json);
                yield break;
            }

            yield return new AsyncSuccess();
        }

        public IEnumerator UpdateFile(File file)
        {
            #region Check the access token is expired
            var check = CheckExpiration();
            while (check.MoveNext())
                yield return null;

            if (check.Current is Exception)
            {
                yield return check.Current;
                yield break;
            }
            #endregion

            var request = new UnityWebRequest("https://www.googleapis.com/drive/v2/files/" + file.ID);
            request.method = "PUT";
            request.headers["Authorization"] = "Bearer " + AccessToken;
            request.headers["Content-Type"] = "application/json";

            string metadata = JsonWriter.Serialize(file.ToJSON());
            request.body = Encoding.UTF8.GetBytes(metadata);

            var response = new UnityWebResponse(request);
            while (!response.isDone)
                yield return null;

            JsonReader reader = new JsonReader(response.text);
Dictionary<string, object> json = reader.Deserialize<Dictionary<string, object>>();

            if (json == null)
            {
                yield return new Exception(-1, "UpdateFile response parsing failed.");
                yield break;
            }
            else if (json.ContainsKey("error"))
            {
                yield return GetError(json);
                yield break;
            }

            yield return new AsyncSuccess(new File(json));
        }


        public IEnumerator TouchFile(File file)
        {
            #region Check the access token is expired
            var check = CheckExpiration();
            while (check.MoveNext())
                yield return null;

            if (check.Current is Exception)
            {
                yield return check.Current;
                yield break;
            }
            #endregion

            var request = new UnityWebRequest("https://www.googleapis.com/drive/v2/files/" +
                    file.ID + "/touch");
            request.method = "POST";
            request.headers["Authorization"] = "Bearer " + AccessToken;
            request.body = new byte[0]; // with no data

            var response = new UnityWebResponse(request);
            while (!response.isDone)
                yield return null;

            JsonReader reader = new JsonReader(response.text);
Dictionary<string, object> json = reader.Deserialize<Dictionary<string, object>>();

            if (json == null)
            {
                yield return new Exception(-1, "TouchFile response parsing failed.");
                yield break;
            }
            else if (json.ContainsKey("error"))
            {
                yield return GetError(json);
                yield break;
            }

            yield return new AsyncSuccess(new File(json));
        }

        public IEnumerator DuplicateFile(File file, string newTitle)
        {
            File newFile = new File(file.ToJSON());
            newFile.Title = newTitle;

            var duplicate = DuplicateFile(file, newFile);
            while (duplicate.MoveNext())
                yield return duplicate.Current;
        }

        public IEnumerator DuplicateFile(File file, string newTitle, File newParentFolder)
        {
            File newFile = new File(file.ToJSON());
            newFile.Title = newTitle;

            // Set the new parent id.
            if (newParentFolder != null)
                newFile.Parents = new List<string> { newParentFolder.ID };
            else
                newFile.Parents = new List<string> { };

            var duplicate = DuplicateFile(file, newFile);
            while (duplicate.MoveNext())
                yield return duplicate.Current;
        }


        IEnumerator DuplicateFile(File file, File newFile)
        {
            #region Check the access token is expired
            var check = CheckExpiration();
            while (check.MoveNext())
                yield return null;

            if (check.Current is Exception)
            {
                yield return check.Current;
                yield break;
            }
            #endregion

            var request = new UnityWebRequest("https://www.googleapis.com/drive/v2/files/" +
                    file.ID + "/copy");
            request.method = "POST";
            request.headers["Authorization"] = "Bearer " + AccessToken;
            request.headers["Content-Type"] = "application/json";

            string metadata = JsonWriter.Serialize(newFile.ToJSON());
            request.body = Encoding.UTF8.GetBytes(metadata);

            var response = new UnityWebResponse(request);
            while (!response.isDone)
                yield return null;

            JsonReader reader = new JsonReader(response.text);
Dictionary<string, object> json = reader.Deserialize<Dictionary<string, object>>();

            if (json == null)
            {
                yield return new Exception(-1, "DuplicateFile response parsing failed.");
                yield break;
            }
            else if (json.ContainsKey("error"))
            {
                yield return GetError(json);
                yield break;
            }

            yield return new AsyncSuccess(new File(json));
        }

        public IEnumerator UploadFile(string title, string parentFolderId, string mimeType, byte[] data)
        {
            File file = new File(new Dictionary<string, object>
                {
                    { "title", title },
                    { "mimeType", mimeType },
                });

            if (!string.IsNullOrEmpty(parentFolderId))
                file.Parents = new List<string> { parentFolderId };

            var upload = UploadFile(file, data);
            while (upload.MoveNext())
                yield return upload.Current;
        }

        public IEnumerator UploadFile(File file, byte[] data)
        {
            #region Check the access token is expired
            var check = CheckExpiration();
            while (check.MoveNext())
                yield return null;

            if (check.Current is Exception)
            {
                yield return check.Current;
                yield break;
            }
            #endregion

            string uploadUrl = null;

            // Start a resumable session.
            if (file.ID == null || file.ID == string.Empty)
            {
                var request = new UnityWebRequest(
                              "https://www.googleapis.com/upload/drive/v2/files?uploadType=resumable");
                request.method = "POST";
                request.headers["Authorization"] = "Bearer " + AccessToken;
                request.headers["Content-Type"] = "application/json";
                request.headers["X-Upload-Content-Type"] = file.MimeType;
                request.headers["X-Upload-Content-Length"] = data.Length;

                string metadata = JsonWriter.Serialize(file.ToJSON());
                request.body = Encoding.UTF8.GetBytes(metadata);

                var response = new UnityWebResponse(request);
                while (!response.isDone)
                    yield return null;

                if (response.statusCode != 200)
                {
                    JsonReader reader = new JsonReader(response.text);
Dictionary<string, object> json = reader.Deserialize<Dictionary<string, object>>();

                    if (json == null)
                    {
                        yield return new Exception(-1, "UploadFile response parsing failed.");
                        yield break;
                    }
                    else if (json.ContainsKey("error"))
                    {
                        yield return GetError(json);
                        yield break;
                    }
                }

                // Save the resumable session URI.
                uploadUrl = response.headers["Location"] as string;
            }
            else
            {
                uploadUrl = "https://www.googleapis.com/upload/drive/v2/files/" + file.ID;
            }

            // Upload the file.
            {
                var request = new UnityWebRequest(uploadUrl);
                request.method = "PUT";
                request.headers["Authorization"] = "Bearer " + AccessToken;
                request.headers["Content-Type"] = "application/octet-stream"; // file.MimeType;
                request.body = data;

                var response = new UnityWebResponse(request);
                while (!response.isDone)
                    yield return null;

                if (string.IsNullOrEmpty(response.text))
                {
                    yield return new Exception(-1, "UploadFile response parsing failed.");
                    yield break;
                }


                JsonReader reader = new JsonReader(response.text);
Dictionary<string, object> json = reader.Deserialize<Dictionary<string, object>>();

                if (json == null)
                {
                    yield return new Exception(-1, "UploadFile response parsing failed.");
                    yield break;
                }
                else if (json.ContainsKey("error"))
                {
                    yield return GetError(json);
                    yield break;
                }

                yield return new AsyncSuccess(new File(json));
            }
        }

       
        public IEnumerator DownloadFile(File file)
        {
            if (file.DownloadUrl == null || file.DownloadUrl == string.Empty)
            {
                yield return new Exception(-1, "No download URL.");
                yield break;
            }

            var download = DownloadFile(file.DownloadUrl);
            while (download.MoveNext())
                yield return download.Current;
        }

       
        public IEnumerator DownloadFile(string url)
        {
            #region Check the access token is expired
            var check = CheckExpiration();
            while (check.MoveNext())
                yield return null;

            if (check.Current is Exception)
            {
                yield return check.Current;
                yield break;
            }
            #endregion

            var request = new UnityWebRequest(url);
            request.headers["Authorization"] = "Bearer " + AccessToken;

            var response = new UnityWebResponse(request);
            while (!response.isDone)
                yield return null;

            yield return new AsyncSuccess(response.bytes);
        }
    }
}