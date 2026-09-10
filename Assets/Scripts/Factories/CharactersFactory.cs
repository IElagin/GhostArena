using System;
using UnityEngine;
using UnityEngine.AI;
using Object = UnityEngine.Object;

namespace GhostArena
{
    public sealed class CharactersFactory
    {
        private readonly GameObject _playerPrefab;
        private readonly GameObject _enemyPrefab;
        private readonly GameObject _projectilePrefab;
        private readonly GameplaySettings _settings;
        private readonly ArenaScene _arenaScene;
        private readonly Transform _runtimeRoot;
        private readonly ControllersFactory _controllersFactory;

        public CharactersFactory(
            GameConfig config,
            GameplaySettings settings,
            ArenaScene arenaScene,
            Transform runtimeRoot,
            ControllersFactory controllersFactory)
        {
            _playerPrefab = config.PlayerPrefab;
            _enemyPrefab = config.EnemyPrefab;
            _projectilePrefab = config.ProjectilePrefab;
            _settings = settings;
            _arenaScene = arenaScene;
            _runtimeRoot = runtimeRoot;
            _controllersFactory = controllersFactory;
        }

        public Character CreatePlayer(Transform spawnPoint)
        {
            if (spawnPoint == null)
            {
                throw new ArgumentNullException(nameof(spawnPoint));
            }

            GameObject characterObject = Object.Instantiate(
                _playerPrefab,
                spawnPoint.position,
                spawnPoint.rotation,
                _runtimeRoot);

            Character character = null;

            try
            {
                character = characterObject.GetComponent<Character>();
                Rigidbody body = characterObject.GetComponent<Rigidbody>();
                WeaponMount weaponMount = characterObject.GetComponent<WeaponMount>();
                CharacterAnimation animation =
                    characterObject.GetComponentInChildren<CharacterAnimation>(true);

                if (character == null || body == null || weaponMount == null || animation == null)
                {
                    throw new InvalidOperationException("Player prefab components are not configured.");
                }

                if (weaponMount.Muzzle == null)
                {
                    throw new InvalidOperationException("Player weapon muzzle is not configured.");
                }

                RigidbodyDirectionalMover mover = new RigidbodyDirectionalMover(
                    body,
                    _settings.PlayerMovementSpeed);
                RigidbodyDirectionalRotator rotator = new RigidbodyDirectionalRotator(body);
                character.Initialize(
                    new Health(_settings.PlayerMaximumHealth),
                    mover,
                    null,
                    rotator);
                Weapon weapon = new Weapon(
                    character,
                    weaponMount.Muzzle,
                    _projectilePrefab,
                    _runtimeRoot,
                    _settings.Projectile);
                character.BindWeapon(weapon);
                Controller controller = _controllersFactory.CreatePlayer(character, _arenaScene.GameplayCamera);
                character.BindController(controller);
                animation.Initialize(character);
                return character;
            }
            catch
            {
                character?.Dispose();
                characterObject.SetActive(false);
                Object.Destroy(characterObject);
                throw;
            }
        }

        public Character CreateEnemy(Transform spawnPoint)
        {
            if (spawnPoint == null)
            {
                throw new ArgumentNullException(nameof(spawnPoint));
            }

            GameObject characterObject = Object.Instantiate(
                _enemyPrefab,
                spawnPoint.position,
                spawnPoint.rotation,
                _runtimeRoot);

            Character character = null;

            try
            {
                character = characterObject.GetComponent<Character>();
                NavMeshAgent agent = characterObject.GetComponent<NavMeshAgent>();
                ContactDamage contactDamage = characterObject.GetComponent<ContactDamage>();
                CharacterAnimation animation =
                    characterObject.GetComponentInChildren<CharacterAnimation>(true);

                if (character == null || agent == null || contactDamage == null || animation == null)
                {
                    throw new InvalidOperationException("Enemy prefab components are not configured.");
                }

                if (agent.isOnNavMesh == false)
                {
                    throw new InvalidOperationException("Enemy navigation agent is not placed on a NavMesh.");
                }

                float rotationSpeed = agent.angularSpeed;
                AgentMover mover = new AgentMover(agent, _settings.Enemy.MovementSpeed);
                TransformDirectionalRotator rotator =
                    new TransformDirectionalRotator(character.transform, rotationSpeed);
                character.Initialize(
                    new Health(_settings.Enemy.MaximumHealth),
                    null,
                    mover,
                    rotator);
                Controller controller = _controllersFactory.CreateEnemy(
                    character,
                    _arenaScene.ArenaHalfExtents,
                    _settings.Enemy.DirectionInterval,
                    _settings.Enemy.MovementSpeed * _settings.Enemy.DirectionInterval);
                character.BindController(controller);
                contactDamage.Initialize(character, _settings.Enemy.ContactDamage);
                animation.Initialize(character);
                return character;
            }
            catch
            {
                character?.Dispose();
                characterObject.SetActive(false);
                Object.Destroy(characterObject);
                throw;
            }
        }

    }
}
