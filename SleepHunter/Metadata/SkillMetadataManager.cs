using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Xml.Serialization;

namespace SleepHunter.Metadata
{
    public sealed class SkillMetadataManager
   {
      public static readonly string SkillMetadataFile = @"Skills.xml";

      #region Singleton
      static readonly SkillMetadataManager instance = new SkillMetadataManager();

      public static SkillMetadataManager Instance { get { return instance; } }

      private SkillMetadataManager() { }
      #endregion

      ConcurrentDictionary<string, SkillMetadata> skills = new ConcurrentDictionary<string, SkillMetadata>(StringComparer.OrdinalIgnoreCase);

      public event SkillMetadataEventHandler SkillAdded;
      public event SkillMetadataEventHandler SkillChanged;
      public event SkillMetadataEventHandler SkillRemoved;

      #region Collection Properties
      public int Count { get { return skills.Count; } }

      public IEnumerable<SkillMetadata> Skills
      {
         get { return from s in skills.Values orderby s.Name ascending select s; }
      }
      #endregion

      #region Collection Methods
      public void AddSkill(SkillMetadata skill)
      {
         if (skill == null)
            throw new ArgumentNullException("skill");

         string skillName = skill.Name.Trim();
         bool wasUpdated = false;

         if (skills.ContainsKey(skillName))
            wasUpdated = true;

         skills[skillName] = skill;

         if (wasUpdated)
            OnSkillChanged(skill);
         else
            OnSkillAdded(skill);
      }

      public bool ContainsSkill(string skillName)
      {
         skillName = skillName.Trim();

         return skills.ContainsKey(skillName);
      }

      public SkillMetadata GetSkill(string skillName)
      {
         skillName = skillName.Trim();

         SkillMetadata skill = null;
         skills.TryGetValue(skillName, out skill);

         return skill;
      }

      public bool RemoveSkill(string skillName)
      {
         skillName = skillName.Trim();

         SkillMetadata removedSkill;
         var wasRemoved = skills.TryRemove(skillName, out removedSkill);

         if (wasRemoved)
            OnSkillRemoved(removedSkill);

         return wasRemoved;
      }

      public bool RenameSkill(string originalName, string newName)
      {
         SkillMetadata skill = null;
         var wasFound = skills.TryRemove(originalName, out skill);

         if (wasFound)
         {
            OnSkillRemoved(skill);
            skills[newName] = skill;
            OnSkillAdded(skill);
         }

         return wasFound;
      }

      public void ClearSkills()
      {
         foreach (var skill in skills.Values)
            OnSkillRemoved(skill);

         skills.Clear();
      }
      #endregion

      #region File Load/Save Methods
      public void LoadFromFile(string filename)
      {
         using (var inputStream = File.Open(filename, FileMode.Open, FileAccess.Read, FileShare.Read))
         {
            LoadFromStream(inputStream);
            inputStream.Close();
         }
      }

      public void LoadFromStream(Stream stream)
      {
         var serializer = new XmlSerializer(typeof(SkillMetadataCollection));
         var collection = serializer.Deserialize(stream) as SkillMetadataCollection;

         if (collection != null)
            foreach (var skill in collection.Skills)
               AddSkill(skill);
      }

      public void SaveToFile(string filename)
      {
         using (var outputStream = File.Create(filename))
         {
            SaveToStream(outputStream);
            outputStream.Flush();
            outputStream.Close();
         }
      }

      public void SaveToStream(Stream stream)
      {
         var collection = new SkillMetadataCollection(this.Skills);
         var serializer = new XmlSerializer(typeof(SkillMetadataCollection));
         var namespaces = new XmlSerializerNamespaces();
         namespaces.Add("", "");

         serializer.Serialize(stream, collection, namespaces);
      }
      #endregion

      #region Event Handler Methods
      void OnSkillAdded(SkillMetadata skill)
      {
         var handler = this.SkillAdded;

         if (handler != null)
            handler(this, new SkillMetadataEventArgs(skill));
      }

      void OnSkillChanged(SkillMetadata skill)
      {
         var handler = this.SkillChanged;

         if (handler != null)
            handler(this, new SkillMetadataEventArgs(skill));
      }

      void OnSkillRemoved(SkillMetadata skill)
      {
         var handler = this.SkillRemoved;

         if (handler != null)
            handler(this, new SkillMetadataEventArgs(skill));
      }
      #endregion
   }
}
