/*
memo layer changed then updYN update
*/
Create Trigger ut_TBL_TalkBoxLayer_Update on TBL_TalkBoxLayer
for  Update 
AS 
Declare @Newmemo nvarchar(4000)
Declare @Oldmemo nvarchar(4000)
Declare @CutFilename nvarchar(150)
Declare @CutFullPath nvarchar(150)

IF update(memo)
BEGIN 
	select @Newmemo = memo, @CutFilename = CutFilename, @CutFullPath = CutFullPath from inserted 
	select @Oldmemo = memo from Deleted
	
	IF (@Newmemo != @Oldmemo)
	BEGIN
		update TBL_TalkBoxLayer set updYN = 'Y'
		where CutFilename = @CutFilename and CutFullPath = @CutFullPath
	END

END 
