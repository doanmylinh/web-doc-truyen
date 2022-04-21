use master
go
if(exists(select * from sys.databases where name = 'QuanLyTruyen')) 
		drop database QuanLyTruyen 
go
create database QuanLyTruyen
go
use QuanlyTruyen
go

create table member
(
	MemberID int identity(1, 1) not null primary key, --typical stuff
	username varchar(128) not null unique,
	passcode varchar(64) not null,
	accounttype int default 0, --0: normal member, 1: admin (post novel)
)
go
insert into member(username, passcode, accounttype) values ('admin','default@admin',1)
insert into member(username, passcode, accounttype) values ('namk','1475369@mkl',1)
go
create table novel
(
	NovelID int identity(1, 1) not null primary key,
	NovelName nvarchar(256) not null unique,
	CoverImage varbinary(max),
	Synopsis nvarchar(max), 
	NovelStatus int default 0, -- 0: Ongoing, 1: Finished
)
go
insert into novel (NovelName, Synopsis, NovelStatus) values (N'Novel 1', N'Database testing', 0 )
insert into novel (NovelName, Synopsis, NovelStatus) values (N'Novel 2', N'Database testing', 1 )
go
create table tag
(
	TagID int identity(1, 1) primary key,
	TagName nvarchar(64) not null,
)
go
insert into tag(TagName) values
(N'Action') /*1*/, (N'Adult') /*2*/, (N'Adventure') /*3*/, (N'Comedy'), /*4*/
(N'Drama') /*5*/, (N'Fantasy') /*6*/, (N'Harem') /*7*/, (N'Historical'), /*8*/
(N'Horror')/*9*/, (N'Martial Arts')/*10*/, (N'Mecha') /*11*/, (N'Mystery'),/*12*/
(N'Psychological') /*13*/,  (N'Romance') /*14*/, (N'School') /*15*/, (N'Sci-fi') /*16*/,
(N'Shoujo') /*17*/,  (N'Shounen') /*18*/, (N'Slice of Life') /*19*/, (N'Sport') ,/*20*/
(N'Supernatural') /*21*/, (N'Tragedy') /*22*/
go



create table noveltag
(
	NovelID int not null,
	TagID int,
	primary key(NovelID, TagID),
	foreign key (NovelID) references novel(NovelID) on update cascade on delete cascade,
	foreign key (TagID) references tag(TagID) on update cascade on delete no action,
)
go
insert into noveltag Values 
(1, 1),
(1, 6),
(1, 10),
(1, 14),
(1, 18),
(2, 2),
(2, 5),
(2, 14),
(2, 15),
(2, 17)
go

create table novelChapter
(
	ChapterID int identity(1, 1) primary key,
	ChapterName nvarchar(128) not null unique,
	ChapterLink nvarchar(max), -- RELATIVE PATH of Chapter text file
	NovelID int not null,
	PrevID int,
	NextID int,
	foreign key (NovelID) references novel(NovelID) on update cascade on delete cascade,
	foreign key (PrevID) references novelChapter(ChapterID) on update no action on delete no action,
	foreign key (NextID) references novelChapter(ChapterID) on update no action on delete no action,
)
go

insert into novelChapter(ChapterName, ChapterLink, NovelID, PrevID, NextID) values 
(N'Novel 1 chapter 1', 'App_Data\1_1.txt', 1, null, null),
(N'Novel 1 chapter 2', 'App_Data\1_2.txt', 1, 1, null),
(N'Novel 2 chapter final', 'App_Data\2_1.txt', 2, null, null)
Go
update novelChapter
Set NextID = 2
Where ChapterID = 1
Go

----------------------------------------------------------------------------------------------------

















