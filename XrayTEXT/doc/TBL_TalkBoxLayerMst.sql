/****** Object:  Table [dbo].[TBL_TalkBoxLayerMst]    Script Date: 2017-11-06 ¿ÀÈÄ 3:00:56 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[TBL_TalkBoxLayerMst](
	[KeyFilename] [nvarchar](150) NOT NULL,
	[isNormalYN] [char](1) NOT NULL,
	[FileTitle] [nvarchar](500) NULL,
	[regdate] [datetime] NOT NULL,
 CONSTRAINT [PK_TBL_TalkBoxLayerMst] PRIMARY KEY CLUSTERED 
(
	[KeyFilename] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO

ALTER TABLE [dbo].[TBL_TalkBoxLayerMst] ADD  CONSTRAINT [DF_TBL_TalkBoxLayerMst_isNormalYN]  DEFAULT ('N') FOR [isNormalYN]
GO

ALTER TABLE [dbo].[TBL_TalkBoxLayerMst] ADD  CONSTRAINT [DF_TBL_TalkBoxLayerMst_regdate]  DEFAULT (getdate()) FOR [regdate]
GO


