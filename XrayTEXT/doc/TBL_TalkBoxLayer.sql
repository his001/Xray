/****** Object:  Table [dbo].[TBL_TalkBoxLayer]    Script Date: 2017-11-08 오후 5:24:11 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[TBL_TalkBoxLayer](
	[idx] [bigint] IDENTITY(1,1) NOT NULL,
	[KeyFilename] [nvarchar](150) NULL,
	[CutFilename] [nvarchar](150) NOT NULL,
	[CutFullPath] [nvarchar](150) NOT NULL,
	[FileTitle] [nvarchar](500) NULL,
	[numb] [int] NULL,
	[memo] [nvarchar](4000) NULL,
	[PointX] [varchar](20) NULL,
	[PointY] [varchar](20) NULL,
	[SizeW] [varchar](20) NULL,
	[SizeH] [varchar](20) NULL,
	[Fileimg] [image] NULL,
	[regdate] [datetime] NOT NULL,
	[updYN] [char](10) NOT NULL,
	[modiDate] [datetime] NULL,
	[sendDate] [datetime] NULL,
	[sendFlag] [nchar](1) NULL,
 CONSTRAINT [PK_TBL_TalkBoxLayer] PRIMARY KEY CLUSTERED 
(
	[CutFilename] ASC,
	[CutFullPath] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO

ALTER TABLE [dbo].[TBL_TalkBoxLayer] ADD  CONSTRAINT [DF_TBL_TalkBoxLayer_regdate]  DEFAULT (getdate()) FOR [regdate]
GO

ALTER TABLE [dbo].[TBL_TalkBoxLayer] ADD  CONSTRAINT [DF_TBL_TalkBoxLayer_updYN]  DEFAULT ('N') FOR [updYN]
GO

ALTER TABLE [dbo].[TBL_TalkBoxLayer] ADD  CONSTRAINT [DF_TBL_TalkBoxLayer_modiDate]  DEFAULT (getdate()) FOR [modiDate]
GO

ALTER TABLE [dbo].[TBL_TalkBoxLayer] ADD  CONSTRAINT [DF_TBL_TalkBoxLayer_sendFlag]  DEFAULT (N'N') FOR [sendFlag]
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'유니크 숫자' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'TBL_TalkBoxLayer', @level2type=N'COLUMN',@level2name=N'idx'
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'원본Xray파일명' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'TBL_TalkBoxLayer', @level2type=N'COLUMN',@level2name=N'KeyFilename'
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'추출한 이미지 파일명' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'TBL_TalkBoxLayer', @level2type=N'COLUMN',@level2name=N'CutFilename'
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'추출된 이미지 파일 경로' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'TBL_TalkBoxLayer', @level2type=N'COLUMN',@level2name=N'CutFullPath'
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Xray설명' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'TBL_TalkBoxLayer', @level2type=N'COLUMN',@level2name=N'FileTitle'
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'내부 순번' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'TBL_TalkBoxLayer', @level2type=N'COLUMN',@level2name=N'numb'
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'소견' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'TBL_TalkBoxLayer', @level2type=N'COLUMN',@level2name=N'memo'
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'시작점X' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'TBL_TalkBoxLayer', @level2type=N'COLUMN',@level2name=N'PointX'
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'시작점Y' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'TBL_TalkBoxLayer', @level2type=N'COLUMN',@level2name=N'PointY'
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'폭' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'TBL_TalkBoxLayer', @level2type=N'COLUMN',@level2name=N'SizeW'
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'높이' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'TBL_TalkBoxLayer', @level2type=N'COLUMN',@level2name=N'SizeH'
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'이미지바이너리' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'TBL_TalkBoxLayer', @level2type=N'COLUMN',@level2name=N'Fileimg'
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'작성일' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'TBL_TalkBoxLayer', @level2type=N'COLUMN',@level2name=N'regdate'
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'메인DB전송여부 Y/N' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'TBL_TalkBoxLayer', @level2type=N'COLUMN',@level2name=N'sendFlag'
GO


