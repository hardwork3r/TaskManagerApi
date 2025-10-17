using MongoDB.Driver;
using MongoDB.Driver.GridFS;
using MongoDB.Bson;

namespace TaskManagerApi.Services;

public class FileService
{
    private readonly IGridFSBucket _gridFsBucket;

    public FileService(IMongoDatabase database)
    {
        _gridFsBucket = new GridFSBucket(database);
    }

    public async Task<string> UploadFileAsync(Stream fileStream, string fileName, string contentType)
    {
        var options = new GridFSUploadOptions
        {
            Metadata = new BsonDocument
            {
                { "fileName", fileName },
                { "contentType", contentType },
                { "uploadDate", DateTime.UtcNow }
            }
        };

        var objectId = await _gridFsBucket.UploadFromStreamAsync(fileName, fileStream, options);
        return objectId.ToString();
    }

    public async Task<(Stream stream, string fileName, string contentType)> DownloadFileAsync(string gridFsId)
    {
        var objectId = new ObjectId(gridFsId);
        var fileInfo = await _gridFsBucket.FindAsync(Builders<GridFSFileInfo>.Filter.Eq("_id", objectId));
        var file = await fileInfo.FirstOrDefaultAsync();

        if (file == null)
            throw new FileNotFoundException("File not found");

        var stream = await _gridFsBucket.OpenDownloadStreamAsync(objectId);
        var fileName = file.Metadata?["fileName"]?.AsString ?? file.Filename;
        var contentType = file.Metadata?["contentType"]?.AsString ?? "application/octet-stream";

        return (stream, fileName, contentType);
    }

    public async Task DeleteFileAsync(string gridFsId)
    {
        var objectId = new ObjectId(gridFsId);
        await _gridFsBucket.DeleteAsync(objectId);
    }
}