/****** Object:  Table [dbo].[TBL_TalkBoxLayerMst]    Script Date: 2017-11-08 오후 5:23:29 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[TBL_TalkBoxLayerMst](
	[KeyFilename] [nvarchar](150) NOT NULL,
	[isNormalYN] [char](1) NOT NULL,
	[FileTitle] [nvarchar](500) NULL,
	[regDate] [datetime] NOT NULL,
	[modiDate] [datetime] NULL,
	[sendDate] [datetime] NULL,
	[sendSubCnt] [smallint] NULL,
	[sendFlag] [nchar](1) NULL,
 CONSTRAINT [PK_TBL_TalkBoxLayerMst] PRIMARY KEY CLUSTERED 
(
	[KeyFilename] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO

ALTER TABLE [dbo].[TBL_TalkBoxLayerMst] ADD  CONSTRAINT [DF_TBL_TalkBoxLayerMst_isNormalYN]  DEFAULT ('N') FOR [isNormalYN]
GO

ALTER TABLE [dbo].[TBL_TalkBoxLayerMst] ADD  CONSTRAINT [DF_TBL_TalkBoxLayerMst_regdate]  DEFAULT (getdate()) FOR [regDate]
GO

ALTER TABLE [dbo].[TBL_TalkBoxLayerMst] ADD  CONSTRAINT [DF_TBL_TalkBoxLayerMst_regDate1]  DEFAULT (getdate()) FOR [modiDate]
GO

ALTER TABLE [dbo].[TBL_TalkBoxLayerMst] ADD  CONSTRAINT [DF_TBL_TalkBoxLayerMst_sendSubCnt]  DEFAULT ((0)) FOR [sendSubCnt]
GO

ALTER TABLE [dbo].[TBL_TalkBoxLayerMst] ADD  CONSTRAINT [DF_TBL_TalkBoxLayerMst_sendFlag]  DEFAULT (N'N') FOR [sendFlag]
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'판독결과 정상소견Y/비정상N' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'TBL_TalkBoxLayerMst', @level2type=N'COLUMN',@level2name=N'isNormalYN'
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'전송한 서브이미지수' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'TBL_TalkBoxLayerMst', @level2type=N'COLUMN',@level2name=N'sendSubCnt'
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'메인DB전송여부 Y/N' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'TBL_TalkBoxLayerMst', @level2type=N'COLUMN',@level2name=N'sendFlag'
GO


