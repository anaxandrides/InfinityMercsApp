namespace InfinityMercsApp.Api.UserDocuments;

/// <summary>A user's file as read, with the ETag a replacement must match.</summary>
public sealed record StoredUserDocument(UserDocument Document, string ETag);
