Console.Write("Please enter drive letter for search, or leave blank to iterate all drives: ");
var driveLetter = Console.ReadLine();
if (string.IsNullOrWhiteSpace(driveLetter))
{

    var drives = DriveInfo.GetDrives();

}
