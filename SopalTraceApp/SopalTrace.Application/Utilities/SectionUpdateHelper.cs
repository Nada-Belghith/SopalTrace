using System;
using System.Collections.Generic;
using System.Linq;

namespace SopalTrace.Application.Utilities
{
    public static class SectionUpdateHelper
    {
        public static void UpdateSections<TSection, TLine, TSectionDto, TLineDto>(
            ICollection<TSection> existingSections,
            IEnumerable<TSectionDto> sectionDtos,
            Action<TSection> deleteSection,
            Action<TLine> deleteLine,
            Func<TSectionDto, Guid?> getSectionDtoId,
            Func<TLineDto, Guid?> getLineDtoId,
            Func<TSection, Guid> getSectionId,
            Func<TLine, Guid> getLineId,
            Func<TSectionDto, TSection> createSection,
            Action<TSection, TSectionDto> updateSection,
            Func<TSection, ICollection<TLine>> getLines,
            Func<TSectionDto, IEnumerable<TLineDto>> getLineDtos,
            Func<TLineDto, TSection, TLine> createLine,
            Action<TLine, TLineDto> updateLine)
            where TSection : class
            where TLine : class
            where TSectionDto : class
            where TLineDto : class
        {
            var sectionDtoIds = sectionDtos.Select(getSectionDtoId).Where(id => id.HasValue).Select(id => id!.Value).ToList();
            var sectionsToRemove = existingSections.Where(s => !sectionDtoIds.Contains(getSectionId(s))).ToList();

            foreach (var sectionToRemove in sectionsToRemove)
            {
                foreach (var lineToRemove in getLines(sectionToRemove).ToList())
                {
                    deleteLine(lineToRemove);
                }
                deleteSection(sectionToRemove);
                existingSections.Remove(sectionToRemove);
            }

            foreach (var sectionDto in sectionDtos)
            {
                var sectionId = getSectionDtoId(sectionDto);
                var existingSection = sectionId.HasValue
                    ? existingSections.FirstOrDefault(s => getSectionId(s) == sectionId.Value)
                    : null;

                if (existingSection != null)
                {
                    updateSection(existingSection, sectionDto);
                }
                else
                {
                    existingSection = createSection(sectionDto);
                    existingSections.Add(existingSection);
                }

                var existingLines = getLines(existingSection);
                var lineDtos = getLineDtos(sectionDto);

                var lineDtoIds = lineDtos.Select(getLineDtoId).Where(id => id.HasValue).Select(id => id!.Value).ToList();
                var linesToRemove = existingLines.Where(l => !lineDtoIds.Contains(getLineId(l))).ToList();

                foreach (var lineToRemove in linesToRemove)
                {
                    deleteLine(lineToRemove);
                    existingLines.Remove(lineToRemove);
                }

                foreach (var lineDto in lineDtos)
                {
                    var lineId = getLineDtoId(lineDto);
                    var existingLine = lineId.HasValue
                        ? existingLines.FirstOrDefault(l => getLineId(l) == lineId.Value)
                        : null;

                    if (existingLine != null)
                    {
                        updateLine(existingLine, lineDto);
                    }
                    else
                    {
                        var newLine = createLine(lineDto, existingSection);
                        existingLines.Add(newLine);
                    }
                }
            }
        }
    }
}
