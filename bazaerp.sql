-- phpMyAdmin SQL Dump
-- version 5.2.1
-- https://www.phpmyadmin.net/
--
-- Host: 127.0.0.1
-- Generation Time: Lis 07, 2026 at 03:14 PM
-- Wersja serwera: 10.4.32-MariaDB
-- Wersja PHP: 8.0.30

SET SQL_MODE = "NO_AUTO_VALUE_ON_ZERO";
START TRANSACTION;
SET time_zone = "+00:00";


/*!40101 SET @OLD_CHARACTER_SET_CLIENT=@@CHARACTER_SET_CLIENT */;
/*!40101 SET @OLD_CHARACTER_SET_RESULTS=@@CHARACTER_SET_RESULTS */;
/*!40101 SET @OLD_COLLATION_CONNECTION=@@COLLATION_CONNECTION */;
/*!40101 SET NAMES utf8mb4 */;

--
-- Database: `bazaerp`
--

-- --------------------------------------------------------

--
-- Struktura tabeli dla tabeli `ewidencjagodzin`
--

CREATE TABLE `ewidencjagodzin` (
  `Id` int(11) NOT NULL,
  `PracownikId` int(11) NOT NULL,
  `Data` date NOT NULL,
  `Godziny` decimal(5,2) NOT NULL,
  `Nadgodziny` decimal(5,2) NOT NULL DEFAULT 0.00,
  `Opis` varchar(255) DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

-- --------------------------------------------------------

--
-- Struktura tabeli dla tabeli `pracownicy`
--

CREATE TABLE `pracownicy` (
  `Id` int(11) NOT NULL,
  `Imie` varchar(50) NOT NULL,
  `Nazwisko` varchar(80) NOT NULL,
  `Email` varchar(150) NOT NULL,
  `Dzial` enum('IT','Ksiegowosc','Kadry','Sprzedaz','Magazyn') NOT NULL,
  `Stanowisko` enum('Praktykant','Pracownik','Specjalista','Kierownik','Dyrektor') NOT NULL,
  `Pensja` decimal(10,2) NOT NULL,
  `DataDodania` datetime NOT NULL DEFAULT current_timestamp()
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

--
-- Indeksy dla zrzutów tabel
--

--
-- Indeksy dla tabeli `ewidencjagodzin`
--
ALTER TABLE `ewidencjagodzin`
  ADD PRIMARY KEY (`Id`),
  ADD KEY `PracownikId` (`PracownikId`);

--
-- Indeksy dla tabeli `pracownicy`
--
ALTER TABLE `pracownicy`
  ADD PRIMARY KEY (`Id`),
  ADD UNIQUE KEY `Email` (`Email`);

--
-- AUTO_INCREMENT for dumped tables
--

--
-- AUTO_INCREMENT for table `ewidencjagodzin`
--
ALTER TABLE `ewidencjagodzin`
  MODIFY `Id` int(11) NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=5;

--
-- AUTO_INCREMENT for table `pracownicy`
--
ALTER TABLE `pracownicy`
  MODIFY `Id` int(11) NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=7;

--
-- Constraints for dumped tables
--

--
-- Constraints for table `ewidencjagodzin`
--
ALTER TABLE `ewidencjagodzin`
  ADD CONSTRAINT `ewidencjagodzin_ibfk_1` FOREIGN KEY (`PracownikId`) REFERENCES `pracownicy` (`Id`) ON DELETE CASCADE;
COMMIT;

/*!40101 SET CHARACTER_SET_CLIENT=@OLD_CHARACTER_SET_CLIENT */;
/*!40101 SET CHARACTER_SET_RESULTS=@OLD_CHARACTER_SET_RESULTS */;
/*!40101 SET COLLATION_CONNECTION=@OLD_COLLATION_CONNECTION */;
