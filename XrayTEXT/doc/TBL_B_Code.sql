/****** Object:  Table [dbo].[TBL_B_Code]    Script Date: 2017-11-06 ���� 2:59:32 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[TBL_B_Code](
	[BCode] [smallint] IDENTITY(1000,1) NOT NULL,
	[BName] [nvarchar](50) NULL,
	[BMemo] [nvarchar](500) NULL,
 CONSTRAINT [PK_B_Code] PRIMARY KEY CLUSTERED 
(
	[BCode] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO


/*
INSERT INTO TBL_B_Code (BName ,BMemo) VALUES ('����','����')
INSERT INTO TBL_B_Code (BName ,BMemo) VALUES ('A�� ����','����')
INSERT INTO TBL_B_Code (BName ,BMemo) VALUES ('B�� ����','����')
INSERT INTO TBL_B_Code (BName ,BMemo) VALUES ('C�� ����','����')
INSERT INTO TBL_B_Code (BName ,BMemo) VALUES ('����','����')
INSERT INTO TBL_B_Code (BName ,BMemo) VALUES ('A������','����')
INSERT INTO TBL_B_Code (BName ,BMemo) VALUES ('B������','����')
INSERT INTO TBL_B_Code (BName ,BMemo) VALUES ('C������','����')
*/