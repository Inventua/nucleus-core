using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nucleus.Abstractions.Models.FileSystem;

namespace Nucleus.Extensions.AmazonS3FileSystemProvider
{
	internal class FileSystemValidationRules
	{
		internal static FileSystemValidationRule[] Root = new FileSystemValidationRule[]
		{
			new FileSystemValidationRule()
			{
				ValidationExpression = "^[0-9a-z]{1}[0-9a-z-]{2,61}[0-9a-z]$" , ErrorMessage = "S3 top level folders (containers) must start and end with a letter or number, contain only letters, numbers and dashes, and must be lower case.",
			},
			new FileSystemValidationRule()
			{
				ValidationExpression = "^(?!.*--)" , ErrorMessage = "S3 top level folders (containers) must not contain consecutive dashes.",
			}
		};

		internal static FileSystemValidationRule[] Bucket = Folder;

		internal static FileSystemValidationRule[] Folder = new FileSystemValidationRule[]
		{
			new FileSystemValidationRule()
			{
				ValidationExpression = "^.{1,1024}$" , ErrorMessage = "S3 sub-folders must be 1-1024 characters long.",
			},
			new FileSystemValidationRule()
			{
				ValidationExpression = "^[^\\/\\\\]+$" , ErrorMessage = "S3 sub-folders cannot contain the '/' or '\\' character.",
			}
		};

		internal static FileSystemValidationRule[] File = new FileSystemValidationRule[]
		{
			new FileSystemValidationRule()
			{
				ValidationExpression = "^.{1,1024}$" , ErrorMessage = "S3 file names must be 1-1024 characters long.",
			},
			new FileSystemValidationRule()
			{
				ValidationExpression = "^[^\\/\\\\]+$" , ErrorMessage = "S3 file names cannot contain the '/' or '\\' character.",
			}
		};

	}
}
